using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed partial class Condition : TreeBranch, ILogicalOperationGroupBranch
{
    private bool? _isMethod;
    private bool? _isComparison;

    private Condition() : base([])
    {
    }

    private Condition(ILogicalOperatorToken logicalOpToken) :
        base([(TokenBase)logicalOpToken]) => LogicalOpToken = logicalOpToken;

    public ITreeElement Left => Children[0];
    public ITreeElement Right => Children[2];

    public ComparisonOperation? Comparison => GetChildOfType<ComparisonOperation>();

    public bool IsComparisonOrEqualityOp => _isComparison ??= Comparison is not null;
    public bool IsFlagCheck => Children.Length == 1;

    public bool IsMethod => _isMethod ??= GetMethod() is not null;

    public ILogicalOperatorToken? LogicalOpToken { get; }

    public Method? GetMethod() => GetChildOfType<Method>();

    public sealed class Parser : ElementParserBase<Condition>
    {
        protected override Type[] ValidParents { get; } = [typeof(WhereRoot), typeof(ConditionGroup)];
        public override IToken[] ExampleTokens => ConditionExampleTokens;

        protected override Condition? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [NameOrValue, Assignment, NameOrValue, ..] => ThrowOnReverseComparison(context),
                [ILogicalOperatorToken conditionalLogicalOp, not LeftCircleBracket, ..] => new Condition(conditionalLogicalOp),
                [Entity, Dot, PropertyAccess, ..] => new Condition(),
                [MethodCall, ..] => new Condition(),
                _ => null
            };

        private static Condition ThrowOnReverseComparison(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString,
                context.Tokens.Take(3).ToArray(),
                ConditionExampleTokens,
                [new And(default)],
                [new Or(default)],
                [new MethodCall(default)]
            );

        public static IToken[] ConditionExampleTokens { get; } =
        [
            new Entity(default), new Dot(default), new PropertyAccess(default), new Equality(default),
            new NameOrValue(default)
        ];
    }

    public sealed class Converter : ElementToExpressionConverter<Condition>
    {
        protected override Expression Build(IBuilderContext context, Condition condition)
        {
            if (condition.IsMethod)
            {
                var property = condition.GetChildOfType<Property>();
                if (property is not null)
                {
                    var prop = context.ToExpression(property);
                    context.MethodSource = (MemberExpression)prop;
                }

                var expression = context.ToExpression(GetMethod(condition));
                context.MethodSource = null;
                return expression;
            }

            if (condition.IsComparisonOrEqualityOp)
            {
                Expression? left = null;
                Expression? right = null;

                if (condition.Left is Property)
                {
                    left = context.ToExpression(condition.Left);
                    context.ComparisonPropertyType = ((PropertyInfo)((MemberExpression)left).Member).PropertyType;
                    right = context.ToExpression(condition.Right);
                    //TODO: maybe it would be better to have transient child context instead and destroy it right after
                    context.ComparisonPropertyType = null;
                }

                if (left is null || right is null)
                {
                    throw new ConditionElementMustHaveLeftAndRightException();
                }

                return condition.Comparison?.Comparison switch
                {
                    null => throw new ComparisonConditionMustHaveComparisonTokenException(),
                    Equality => Expression.Equal(left, right),
                    GreaterOrEqualThan => Expression.GreaterThanOrEqual(left, right),
                    GreaterThan => Expression.GreaterThan(left, right),
                    Inequality => Expression.NotEqual(left, right),
                    LessThan => Expression.LessThan(left, right),
                    LessOrEqualThan => Expression.LessThanOrEqual(left, right),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            if (condition.IsFlagCheck)
            {
                var left = context.ToExpression(condition.Left);
                if (left.Type == typeof(bool))
                {
                    return left;
                }
            }

            throw new NotSupportedConditionException();
        }

        private static Method GetMethod(Condition condition) =>
            condition.GetChildOfType<Method>() ?? throw new ConditionMethodChildElementMissingException();
    }
}