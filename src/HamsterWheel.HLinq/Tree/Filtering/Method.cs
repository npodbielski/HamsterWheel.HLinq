using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed partial class Method(IToken[] tokens) : TreeBranch(tokens), IMethod
{
    private MethodSource? _source;

    private IToken First => Tokens.First() is Assignment ? Tokens[1] : Tokens.First();

    private MethodSource SourceType => _source ??= First switch
    {
        Entity => MethodSource.Entity,
        Dot => MethodSource.Property,
        NameOrValue => MethodSource.Constant,
        _ => MethodSource.Static
    };

    private MethodName MethodName => field ??= Tokens.OfType<MethodName>().First();

    public string GetName(string hLinqQuery) => MethodName.GetValue(hLinqQuery);

    /// <summary>
    /// Whether the call stands on the entity of the query itself (`x.Count()`) rather than on a
    /// property or a static class - after a grouping the entity of the clause is the group.
    /// </summary>
    public bool IsEntity => SourceType == MethodSource.Entity;

    /// <summary>
    /// The property argument of the call (<c>x.Age</c> in <c>x.Max(x.Age)</c>) when it has one.
    /// </summary>
    private Property? Argument => Children.OfType<Property>().SingleOrDefault();

    public string[]? ArgumentPath(string hLinqQuery) => Argument?.GetPath(hLinqQuery);

    /// <summary>
    /// The arguments the query wrote between the brackets, with the expression of every argument
    /// that names a member: the ranking of an overload asks both - what a written value says and
    /// what type a member of the row is.
    /// </summary>
    public (string value, MemberExpression? expression)[] WrittenParameters(IBuilderContext context) =>
        [.. Children.OfType<IMethodParamElement>()
            .Select(p => (value: p.GetValue(context.HLinqQuery),
                expression: p is Property property ? context.ToExpression(property) as MemberExpression : null))];

    /// <summary>
    /// The overload of this call among the instance methods of <paramref name="source"/>, refused
    /// with the signatures it could have been.
    /// </summary>
    public MethodInfo ResolveInstanceMethod(IBuilderContext context, Type source, string memberName)
    {
        var parameters = WrittenParameters(context);
        var name = GetName(context.HLinqQuery);
        var resolution = context.Builder.ResolveMethod(source, name, parameters, context.Culture);
        resolution.Report(context);
        return resolution.Member ??
               throw new InvalidMethodException(source, memberName, name,
                   [.. parameters.Select(p => p.value)], [.. resolution.Competitors]);
    }

    /// <summary>
    /// The overload of this call among the static functions the provider packs hand to a query,
    /// refused with the signatures it could have been.
    /// </summary>
    public MethodInfo ResolveStaticMethod(IBuilderContext context)
    {
        var parameters = WrittenParameters(context);
        var name = GetName(context.HLinqQuery);
        var resolution = context.Builder.ResolveStaticMethod(name, parameters, context.Culture);
        resolution.Report(context);
        return resolution.Member ??
               throw new InvalidStaticMethodException(name, [.. parameters.Select(p => p.value)],
                   [.. resolution.Competitors]);
    }

    // ReSharper disable once UnusedType.Global -> used by the Parser
    public sealed class Parser : ElementParserBase<Method>
    {
        protected override Type[] ValidParents { get; } =
        [
            typeof(Condition), typeof(Selecting.PropertyAssignment),
            typeof(Ordering.OrderByRoot), typeof(Ordering.OrderByDescendingRoot),
            typeof(Ordering.ThenByRoot), typeof(Ordering.ThenByDescendingRoot)
        ];

        public override IToken[] ExampleTokens { get; } =
            [MethodName.Empty, LeftCircleBracket.Empty, RightCircleBracket.Empty];

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightCircleBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }

        protected override Method? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Assignment, Entity, Dot, MethodName _, LeftCircleBracket, ..] => new Method(context.Tokens[..5]),
                [Entity, Dot, MethodName _, LeftCircleBracket, ..] => new Method(context.Tokens[..4]),
                [Dot, MethodName _, LeftCircleBracket, ..] => new Method(context.Tokens[..3]),
                [MethodName _, LeftCircleBracket, ..]
                    when context.CurrentBranch is Condition or Selecting.PropertyAssignment => new Method(
                    context.Tokens[..2]),
                [Assignment, MethodName _, LeftCircleBracket, ..]
                    when context.CurrentBranch is Selecting.PropertyAssignment => new Method(
                    context.Tokens[..3]),
                _ => null
            };
    }

    public sealed class Converter(
        IPropertyMethodToExpressionConverter propertyMethodConverter,
        IStaticMethodToExpressionConverter staticMethodConverter,
        IAggregateMethodToExpressionConverter aggregateMethodConverter) : ElementToExpressionConverter<Method>
    {
        protected override Expression Build(IBuilderContext context, Method method)
        {
            var parametersConverter = context.Builder.GetParametersConverter();
            switch (method.SourceType)
            {
                case MethodSource.Entity:
                    return aggregateMethodConverter.BuildOverEntity(context, method);
                case MethodSource.Property:
                    return propertyMethodConverter.BuildInstance(context, method, parametersConverter);
                case MethodSource.Static:
                    return staticMethodConverter.BuildStatic(context, method, parametersConverter);
                case MethodSource.Constant:
                    break;
            }

            throw new InvalidMethodQueryException(method.GetName(context.HLinqQuery));
        }
    }
}