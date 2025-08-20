using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filter;
using PropertyAccessToken = HamsterWheel.HLinq.Tokens.Filter.PropertyAccess;

namespace HamsterWheel.HLinq.Tree.Filter;

public sealed class Condition : TreeBranch, ILogicalOperationGroupBranch
{
    private bool? _isMethod;
    private bool? _isComparison;

    private Condition() : base([])
    {
    }

    private Condition(IConditionalLogicalOperationToken conditionalLogicalOp) :
        base([(TokenBase)conditionalLogicalOp]) => ConditionalLogicalOp = conditionalLogicalOp;

    public ITreeElement Left => Children[0];
    public ITreeElement Right => Children[2];

    public ComparisonOperation? Comparison => GetChildOfType<ComparisonOperation>();

    public bool IsComparisonOrEqualityOp => _isComparison ??= Comparison is not null;
    public bool IsFlagCheck => Children.Length == 1;

    public bool IsMethod => _isMethod ??= GetMethod() is not null;

    public IConditionalLogicalOperationToken? ConditionalLogicalOp { get; }

    public Method? GetMethod() => GetChildOfType<Method>();

    public sealed class Parser : ElementParserBase<Condition>
    {
        protected override Type[] ValidParents => [typeof(WhereRoot), typeof(ConditionGroup)];

        protected override Condition? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [IConditionalLogicalOperationToken conditionalLogicalOp, ..] => new Condition(conditionalLogicalOp),
                [Entity, Dot, PropertyAccessToken, ..] => new Condition(),
                [MethodCall, ..] => new Condition(),
                //TODO: this will not work with filter like where[[1,2,3].Contains(x.Id)] -> probably condition should be split into multiple subclasses with different implementations for easier parsing and conversion to expression
                [NameOrValue _, IComparisonToken, ..] => new Condition(),
                _ => null
            };
        }
    }

    public sealed class Converter : ElementToExpressionConverter<Condition>
    {
        protected override Expression Build(IBuilderContext context, Condition condition)
        {
            if (condition.IsMethod)
            {
                var property = GetProp(condition);
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
                    throw new InvalidOperationException(
                        "Cannot build comparison expression if right or left expression is null");

                return condition.Comparison?.Comparison switch
                {
                    null => throw new NotImplementedException(
                        "Condition that is comparison need to have comparison token."),
                    //TODO: probably for == and floating point numbers it should include precision
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

            throw new NotImplementedException(
                "Condition need to be either method call: 'x.Name.StartsWith(abc)' or comparison 'x.Name==abc'");
        }

        private static Property? GetProp(Condition condition) => condition.GetChildOfType<Property>();

        private static Method GetMethod(Condition condition) =>
            condition.GetChildOfType<Method>() ??
            throw new InvalidDataException(
                "Condition is method call condition but method child does not exits");
    }
}