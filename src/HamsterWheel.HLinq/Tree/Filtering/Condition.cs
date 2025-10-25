using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Pipeline.Parser;
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

    public ILogicalOperatorToken? LogicalOpToken { get; }

    private Condition(ILogicalOperatorToken logicalOpToken) :
        base([(TokenBase)logicalOpToken]) => LogicalOpToken = logicalOpToken;

    private ITreeElement Left => Children[0];
    private ITreeElement Right => Children[2];

    private ComparisonOperation? Comparison => GetChildOfType<ComparisonOperation>();

    private bool IsComparisonOrEqualityOp => _isComparison ??= Comparison is not null;
    private bool IsFlagCheck => Children.Length == 1;

    private bool IsMethod => _isMethod ??= GetMethod() is not null;

    private Method? GetMethod() => GetChildOfType<Method>();

    public sealed class Parser : ElementParserBase<Condition>
    {
        protected override Type[] ValidParents { get; } = [typeof(WhereRoot), typeof(ConditionGroup)];
        public override IToken[] ExampleTokens => ConditionExampleTokens;

        public static IToken[] ConditionExampleTokens { get; } =
            [Entity.Empty, Dot.Empty, PropertyName.Empty, Equality.Empty, NameOrValue.Empty];

        protected override Condition? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [NameOrValue, Assignment, NameOrValue, ..] => ThrowOnReverseComparison(context),
                [ILogicalOperatorToken conditionalLogicalOp, not LeftCircleBracket, ..] => new Condition(
                    conditionalLogicalOp),
                [Entity, Dot, PropertyName, ..] => new Condition(),
                [Entity, ..] => new Condition(),
                [MethodName, ..] => new Condition(),
                _ => null
            };

        private static Condition ThrowOnReverseComparison(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString,
                context.Tokens.Take(3).ToArray(),
                ConditionExampleTokens,
                [And.Empty],
                [Or.Empty],
                [MethodName.Empty]
            );
    }

    public sealed class Converter(IConverterFactory converterFactory) : ElementToExpressionConverter<Condition>
    {
        protected override Expression Build(IBuilderContext context, Condition condition)
        {
            if (condition.IsMethod)
            {
                var property = condition.GetChildOfType<Property>();
                var methodCallContext = new MethodCallConditionBuilderContext(context);
                if (property is not null)
                {
                    methodCallContext.ToExpression(property);
                }

                var expression = methodCallContext.ToExpression(GetMethod(condition));
                return expression;
            }

            if (condition.IsComparisonOrEqualityOp)
            {
                Expression? left = null;
                Expression? right = null;

                if (condition.Left is Property property)
                {
                    var conditionBuilderContext = new ArithmeticComparisonConditionBuilderContext(context);
                    left = converterFactory.GetToExpressionConverterFor<Property>()
                        .Build(conditionBuilderContext, property);
                    right = conditionBuilderContext.ToExpression(condition.Right);
                }
                else if (condition.Left is EntityLeaf)
                {
                    var conditionBuilderContext = new ArithmeticComparisonConditionBuilderContext(context)
                    {
                        ComparisonType = context.Type
                    };
                    left = context.Param;
                    right = conditionBuilderContext.ToExpression(condition.Right);
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