using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Grouping;
using static System.Linq.Expressions.Expression;

namespace HamsterWheel.HLinq.Tree.Selecting;

public sealed partial class PropertyAssignment(IToken[] tokens) : TreeBranch(tokens), IPropertyAssignment
{
    private string? _value;

    public bool NeedsObjectInitializer =>
        NewPropName != null || NestedSelect is not null || Call is not null || IsMathExpression;

    public IProperty? NestedSelectProperty => field ??= NestedSelect is null
        ? null
        : Property;

    public IInnerProperty? NestedSelectInnerProperty => field ??= NestedSelect is null
        ? null
        : Children.OfType<InnerProperty>().FirstOrDefault();

    /// <summary>
    /// A call written as the value of this assignment (<c>C=x.Count()</c>, <c>N=x.Addresses.Count()</c>).
    /// </summary>
    internal Method? Call => field ??= Children.OfType<Method>().SingleOrDefault();

    /// <summary>
    /// Whether the query wrote an operator in this member: its value is an expression.
    /// </summary>
    public bool IsMathExpression => Children.Any(child => child is MathTerm or MathGroup);

    /// <summary>
    /// The call standing on the entity of the clause - after a grouping, the group's aggregate.
    /// </summary>
    public IMethod? GroupAggregate => Call is { IsGroupAggregate: true } call && !IsMathExpression ? call : null;

    public INestedSelect? NestedSelect => field ??= Children.OfType<INestedSelect>().FirstOrDefault();

    private IInnerProperty? InnerProperty => field ??= Children.OfType<IInnerProperty>().FirstOrDefault();

    private IInitializerPropertyName? NewPropName =>
        field ??= Children.OfType<IInitializerPropertyName>().FirstOrDefault();

    private Property? Property => field ??= this.GetChildOfType<Property>();

    /// <summary>
    /// The name the query wrote for this member, null when it wrote none.
    /// </summary>
    public string? ExplicitPropertyName(string hLinqQuery) => NewPropName?.GetName(hLinqQuery);

    /// <summary>
    /// The name a call answers under: the one the query wrote, or the method's own.
    /// </summary>
    public string? FinalPropertyName(string hLinqQuery) =>
        ExplicitPropertyName(hLinqQuery)
        ?? (IsMathExpression
            ? null
            : Call?.GetName(hLinqQuery)
                ?? Property?.GetLastName(hLinqQuery)
                ?? InnerProperty?.GetLastName(hLinqQuery));

    private string GetValue(string hLinqQuery) =>
        _value ??= (Children.OfType<InitializerConstantValue>().FirstOrDefault() ??
                    throw new PropertyAssignmentValueCannotBeResolvedException()).ValueToken.GetValue(hLinqQuery);

    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser : ElementParserBase<PropertyAssignment>
    {
        public override Type[] ValidParents { get; } =
        [
            typeof(SelectRoot),
            typeof(GroupByRoot),
            typeof(NestedSelect)
        ];

        public override IToken[] ExampleTokens { get; } = [Entity.Empty, Dot.Empty, PropertyName.Empty];

        protected override PropertyAssignment? BuildBranch(IParsingContext context)
        {
            switch (context.Tokens)
            {
                case [NameOrValue, Comma or RightCircleBracket, ..] when context.CurrentBranch is NestedSelect:
                case [Comma, NameOrValue, RightCircleBracket, ..] when context.CurrentBranch is NestedSelect:
                    return new PropertyAssignment([]);
                case [Comma, NameOrValue, Assignment, ..]:
                case [Comma, Entity, Dot, ..]:
                case [Comma, SubEntity, Dot, ..]:
                    return new PropertyAssignment([context.Tokens[0]]);
                case [NameOrValue, Assignment, ..]:
                case [LeftCircleBracket, ..]:
                case [Entity, Dot, ..]:
                case [SubEntity, Dot, ..]:
                case [MethodName, LeftCircleBracket, ..]:
                    return new PropertyAssignment([]);
                default:
                    return null;
            }
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class MemberBindingConverter(
        IPropertiesCache props,
        IDefaultConverter converter,
        INestedSelectBuilder nestedSelects,
        IAggregateMethodToExpressionConverter aggregates,
        IAggregatesCache aggregatesCache,
        IPropertyMethodToExpressionConverter instanceMethods,
        IStaticMethodToExpressionConverter staticMethods) : ElementToMemberAssignmentConverter<PropertyAssignment>
    {
        protected override PropInfo GetPropInfo(IBuilderContext context, PropertyAssignment element)
        {
            if (element.IsMathExpression)
            {
                var expressionName = element.NewPropName?.GetName(context.HLinqQuery) ??
                                     throw new MathExpressionNeedsANameException();
                return new(expressionName, context.Builder.FoldMath(context, element.Children).Type);
            }

            if (element.NestedSelect is { } nested)
            {
                var name = element.NewPropName?.GetName(context.HLinqQuery) ??
                           TargetProperty(element, context) ??
                           throw new PropertyAssignmentNameNotFoundException(element, context.HLinqQuery);
                return new(name, nestedSelects.GetResultType(element, nested, context));
            }

            if (element.Call is { } call)
            {
                var callName = element.FinalPropertyName(context.HLinqQuery) ?? call.GetName(context.HLinqQuery);
                return new(callName, MethodCallType(context, element, call));
            }

            if (element.InnerProperty is { } inner && context is INestedScopeBuilderContext innerScope)
            {
                var innerName = element.NewPropName?.GetName(context.HLinqQuery) ??
                                inner.GetLastName(context.HLinqQuery);
                return new(innerName, InnerMemberType(innerScope, inner));
            }

            if (element.Property is null)
            {
                var name = element.NewPropName?.GetName(context.HLinqQuery) ??
                           throw new PropertyAssignmentNameNotFoundException(element, context.HLinqQuery);
                return new(name, TryToFindBestType(context, element));
            }

            var path = element.Property.GetPath(context.HLinqQuery);
            var expression = context.Builder.GetProperty(context.Type, context.Param, path, context).Member;
            var propertyName = element.NewPropName?.GetName(context.HLinqQuery) ?? expression.Member.Name;
            return new(propertyName, expression.Type);
        }

        protected override MemberAssignment Build(IBuilderContext context, PropertyAssignment element,
            Type destinationType)
        {
            if (element.IsMathExpression)
            {
                var expressionName = element.NewPropName?.GetName(context.HLinqQuery) ??
                                     throw new MathExpressionNeedsANameException();
                var expressionResolution = props.Resolve(destinationType, expressionName);
                expressionResolution.Report(context);
                var expressionProperty = expressionResolution.Member ??
                                         throw new InvalidPropertyPathException(destinationType, expressionName,
                                             [.. destinationType.GetProperties().Select(p => p.Name)]);
                return Bind(expressionProperty, context.Builder.FoldMath(context, element.Children));
            }

            if (element.Call is { } call)
            {
                var callName = element.FinalPropertyName(context.HLinqQuery) ?? call.GetName(context.HLinqQuery);
                var callResolution = props.Resolve(destinationType, callName);
                callResolution.Report(context);
                var callProperty = callResolution.Member ??
                                   throw new InvalidPropertyPathException(destinationType, callName,
                                       [.. destinationType.GetProperties().Select(p => p.Name)]);
                return Bind(callProperty, MethodCall(context, element, call));
            }

            var propName = element.NewPropName?.GetName(context.HLinqQuery) ??
                           TargetProperty(element, context) ??
                           throw new PropertyAssignmentNameNotFoundException(element, context.HLinqQuery);
            var resolution = props.Resolve(destinationType, propName);
            resolution.Report(context);
            var propertyInfo = resolution.Member ??
                               throw new InvalidPropertyPathException(destinationType, propName,
                                   [.. destinationType.GetProperties().Select(p => p.Name)]);
            if (element.NestedSelect is { } nested)
            {
                return Bind(propertyInfo, nestedSelects.Build(element, nested, context));
            }

            if (element.Property is not null)
            {
                var initializerContext = new InitializerPropertyAssignmentBuilderContext(context)
                {
                    InitializerPropertyType = propertyInfo.PropertyType
                };
                var expression = (MemberExpression)initializerContext.Builder.ToExpression(context, element.Property);
                return Bind(propertyInfo, expression);
            }

            if (element.InnerProperty is not null)
            {
                return Bind(propertyInfo, context.ToExpression(element.InnerProperty));
            }

            //if this is constant value assignment just assign it
            var value = TransformValueForType(context, element, propertyInfo);
            return Bind(propertyInfo,
                Constant(converter.ConvertTo(propertyInfo.PropertyType, value, context.Culture)));
        }

        private Expression MethodCall(IBuilderContext context, PropertyAssignment element,
            Method call)
        {
            var parametersConverter = context.Builder.GetParametersConverter();
            if (call.IsEntity)
            {
                return aggregates.BuildOverEntity(context, call);
            }

            if (element.Property is null && element.InnerProperty is null)
            {
                return staticMethods.BuildStatic(context, call, parametersConverter);
            }

            var callContext = new MethodCallConditionBuilderContext(context)
            {
                MethodSource = CallSource(context, element)
            };
            return instanceMethods.BuildInstance(callContext, call, parametersConverter);
        }

        private Type MethodCallType(IBuilderContext context, PropertyAssignment element, Method call)
        {
            if (call.IsEntity)
            {
                return aggregates.ReturnTypeOverSubject(context, call);
            }

            if (element.Property is null && element.InnerProperty is null)
            {
                return call.ResolveStaticMethod(context).ReturnType;
            }

            var source = CallSource(context, element);
            return aggregatesCache.IsAggregateName(call.GetName(context.HLinqQuery))
                ? aggregates.ReturnType(context, call, source.Type)
                : call.ResolveInstanceMethod(context, source.Type, MemberName(element, context)).ReturnType;
        }

        private static string MemberName(PropertyAssignment element, IBuilderContext context) =>
            element.Property?.GetLastName(context.HLinqQuery) ??
            element.InnerProperty?.GetLastName(context.HLinqQuery) ??
            throw new PropertyAssignmentWithoutPropertyCannotBeTranslatedException();

        private static MemberExpression CallSource(IBuilderContext context, PropertyAssignment element)
        {
            if (element.InnerProperty is { } inner && context is INestedScopeBuilderContext scope)
            {
                return scope.Builder
                    .GetProperty(scope.InnerType, scope.InnerSource, inner.GetPath(scope.HLinqQuery), scope)
                    .Member;
            }

            var property = element.Property ?? throw new PropertyAssignmentWithoutPropertyCannotBeTranslatedException();
            var path = property.GetPath(context.HLinqQuery);
            return context.Builder.GetProperty(context.Type, context.Param, path, context).Member;
        }

        private static Type InnerMemberType(INestedScopeBuilderContext scope, IInnerProperty inner) =>
            scope.Builder
                .GetProperty(scope.InnerType, scope.InnerSource, inner.GetPath(scope.HLinqQuery), scope)
                .Member.Type;

        /// <summary>
        /// The name of the property the member targets: the last segment of the path it is written
        /// on - <c>contact</c> in <c>x.customer.address.contact</c>, <c>City</c> in <c>a.City</c>.
        /// Null when the member is a constant assignment, which is named by
        /// <see cref="InitializerPropertyName"/>.
        /// </summary>
        private static string? TargetProperty(PropertyAssignment element, IBuilderContext context) =>
            element.Property?.GetLastName(context.HLinqQuery) ??
            element.InnerProperty?.GetLastName(context.HLinqQuery);

        private static string TransformValueForType(IBuilderContext context, PropertyAssignment element,
            PropertyInfo propertyInfo)
        {
            var value = element.GetValue(context.HLinqQuery);
            if (propertyInfo.PropertyType == typeof(char) && value.IsSingleQuoted())
            {
                return value.UnQuote();
            }

            return value;
        }

        private static Type TryToFindBestType(IBuilderContext context, PropertyAssignment element)
        {
            var value = element.GetValue(context.HLinqQuery);
            if (value.IsDoubleQuoted())
            {
                return typeof(string);
            }

            switch (value.Length)
            {
                case 1 when char.IsDigit(value[0]):
                case > 1 when value.All(char.IsDigit):
                    return typeof(int);
                case > 1 when value.All(c => char.IsDigit(c) || char.IsPunctuation(c)) &&
                              value.Count(char.IsPunctuation) == 1:
                    return typeof(double);
                case 1 when char.IsLetter(value[0]):
                case 3 when value.IsSingleQuoted():
                    return typeof(char);
            }

            return typeof(string);
        }

        public class PropertyAssignmentNameNotFoundException(PropertyAssignment element, string hLinqQuery)
            : HLinqQueryException(
                $"Could not find property with name of '{element.NewPropName?.GetName(hLinqQuery) ?? element.Property?.GetValue(hLinqQuery)}'");
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ExpressionConverter : ElementToExpressionConverter<PropertyAssignment>
    {
        protected override Expression Build(IBuilderContext context, PropertyAssignment element) =>
            element.Property is not null
                ? (MemberExpression)context.Builder.ToExpression(context, element.Property)
                : throw new PropertyAssignmentWithoutPropertyCannotBeTranslatedException();
    }
}