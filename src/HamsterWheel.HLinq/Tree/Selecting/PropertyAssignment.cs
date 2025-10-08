using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

public sealed class PropertyAssignment(IToken[] tokens) : TreeBranch(tokens)
{
    private Property? _property;
    private InitializerPropertyName? _name;
    private string? _value;

    public bool NeedsObjectInitializer => NewPropName != null;

    private Property? Property => _property ??= Children.OfType<Property>().FirstOrDefault();

    private InitializerPropertyName? NewPropName =>
        _name ??= Children.OfType<InitializerPropertyName>().FirstOrDefault();

    private string GetValue(string hLinqQuery) =>
        _value ??= (Children.OfType<InitializerConstantValue>().FirstOrDefault() ??
                    throw new PropertyAssignmentValueCannotBeResolvedException()).ValueToken.GetValue(hLinqQuery);

    public sealed class Parser : ElementParserBase<PropertyAssignment>
    {
        protected override Type[] ValidParents { get; } = [typeof(SelectRoot)];

        protected override PropertyAssignment? BuildBranch(IParsingContext context)
        {
            var index = 0;

            if (context.Tokens is [Comma, ..])
            {
                index = 1;
            }

            if (context.Tokens[index..] is [NameOrValue, Assignment, ..] ||
                context.Tokens[index..] is [Entity, Dot, ..])
            {
                return new PropertyAssignment(context.Tokens[..index]);
            }

            return null;
        }
    }

    public sealed class MemberBindingConverter(IPropertiesCache props, IDefaultConverter converter)
        : ElementToMemberAssignmentConverter<PropertyAssignment>
    {
        protected override PropInfo GetPropInfo(IBuilderContext context, PropertyAssignment element)
        {
            if (element.Property is null)
            {
                return new(element.NewPropName?.GetName(context.HLinqQuery) ??
                           throw new PropertyAssignmentNameNotFoundException(element, context.HLinqQuery),
                    TryToFindBestType(context, element));
            }

            var path = element.Property.GetPath(context.HLinqQuery);
            var expression = context.Builder.GetPropertyWithType(context.Type, context.Param, path).Member;
            var propertyName = element.NewPropName?.GetName(context.HLinqQuery) ?? expression.Member.Name;
            return new(propertyName, expression.Type);
        }

        protected override MemberAssignment Build(IBuilderContext context, PropertyAssignment element,
            Type destinationType)
        {
            var propName = element.NewPropName?.GetName(context.HLinqQuery) ??
                           element.Property?.GetPath(context.HLinqQuery).LastOrDefault() ??
                           throw new PropertyAssignmentNameNotFoundException(element, context.HLinqQuery);
            var propertyInfo = props.Single(destinationType, propName) ??
                               throw new InvalidPropertyPathException(destinationType, propName,
                                   [..destinationType.GetProperties().Select(p => p.Name)]);
            if (element.Property is not null)
            {
                context.InitializerPropertyType = propertyInfo.PropertyType;
                var expression = (MemberExpression)context.Builder.ToExpression(context, element.Property);
                context.InitializerPropertyType = null;
                return Expression.Bind(propertyInfo, expression);
            }

            //if this is constant value assignment just assign it
            var value = TransformValueForType(context, element, propertyInfo);
            return Expression.Bind(propertyInfo,
                Expression.Constant(converter.ConvertTo(propertyInfo.PropertyType, value)));
        }

        private static string TransformValueForType(IBuilderContext context, PropertyAssignment element,
            PropertyInfo propertyInfo)
        {
            var value = element.GetValue(context.HLinqQuery);
            if (propertyInfo.PropertyType == typeof(string) && value is ['"', .., '"'])
            {
                return value[1..^1];
            }

            if (propertyInfo.PropertyType == typeof(char) && value is ['\'', .., '\''])
            {
                return value[1..^1];
            }

            return value;
        }

        private static Type TryToFindBestType(IBuilderContext context, PropertyAssignment element)
        {
            var value = element.GetValue(context.HLinqQuery);
            if (value is ['"', .., '"'])
            {
                return typeof(string);
            }

            //TODO: probably it should be possible to annotate via the client what kind of type it is expecting. or for server to return this property with the type somehow
            //..in Linq it is pretty clear what is expected but when we translate it to hLinq this information is lost
            //..to make it work seamlessly via http and without the surprises this information should be carried to the server too
            switch (value.Length)
            {
                case 1 when char.IsDigit(value[0]):
                case > 1 when value.All(char.IsDigit):
                    return typeof(int);
                case > 1 when value.All(c => char.IsDigit(c) || char.IsPunctuation(c)) &&
                              value.Count(char.IsPunctuation) == 1:
                    return typeof(double);
                case 1 when char.IsLetter(value[0]):
                case 3 when value is ['\'', .., '\'']:
                    return typeof(char);
                //TODO: dates
            }

            return typeof(string);
        }

        public class PropertyAssignmentNameNotFoundException(PropertyAssignment element, string hLinqQuery)
            : HLinqQueryException(
                $"Could not find property with name of '{element.NewPropName?.GetName(hLinqQuery) ?? element.Property?.GetValue(hLinqQuery)}'");
    }

    public class ExpressionConverter : ElementToExpressionConverter<PropertyAssignment>
    {
        protected override Expression Build(IBuilderContext context, PropertyAssignment element) =>
            element.Property is not null
                ? (MemberExpression)context.Builder.ToExpression(context, element.Property)
                : throw new PropertyAssignmentWithoutPropertyCannotBeTranslatedException();

        public class PropertyAssignmentWithoutPropertyCannotBeTranslatedException()
            : HLinqQueryException(
                $"{nameof(PropertyAssignment)} cannot be translated to Expression if it does not contain source '{nameof(Property)}' information.");
    }

    public class PropertyAssignmentValueCannotBeResolvedException()
        : HLinqQueryException(
            $"{nameof(PropertyAssignment)} provide value of '{nameof(NameOrValue)}' token if it was not part of the parsed tree. " +
            $"Make sure that your code is trying to translate query into expression correctly");
}