using System.Linq.Expressions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

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

    private ComparisonOperation? Comparison => this.GetChildOfType<ComparisonOperation>();
    private bool IsComparisonOrEqualityOp => _isComparison ??= Comparison is not null;
    private bool IsFlagCheck => Children.Length == 1;
    private bool IsMethod => _isMethod ??= GetMethod() is not null;
    public ILogicalOperatorToken? LogicalOpToken { get; }

    private Method? GetMethod() => this.GetChildOfType<Method>();

    /// <summary>
    /// The children split at the comparison operator, each side in the order the query wrote.
    /// </summary>
    private (IReadOnlyList<ITreeElement> Left, IReadOnlyList<ITreeElement> Right) Sides()
    {
        var operatorAt = Array.IndexOf(Children, Comparison ??
                                                 throw new ComparisonConditionMustHaveComparisonTokenException());
        return ([.. Children.Take(operatorAt)], [.. Children.Skip(operatorAt + 1)]);
    }

    public sealed class Parser : ElementParserBase<Condition>
    {
        public override Type[] ValidParents { get; } = [typeof(WhereRoot), typeof(ConditionGroup)];
        public override IToken[] ExampleTokens => ConditionExampleTokens;

        public static IToken[] ConditionExampleTokens { get; } =
            [Entity.Empty, Dot.Empty, PropertyName.Empty, Equality.Empty, NameOrValue.Empty];

        protected override Condition? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                //a value on the left of a comparison (`where[10<x.Int]`): the comparison keeps the
                //order it was written in, so it does not have to be mirrored to be understood
                [NameOrValue, IComparisonToken, ..] => new Condition(),
                [ILogicalOperatorToken conditionalLogicalOp, not LeftCircleBracket, ..] => new Condition(
                    conditionalLogicalOp),
                [Entity, Dot, PropertyName, ..] => new Condition(),
                [Entity, ..] => new Condition(),
                [MethodName, ..] => new Condition(),
                _ => null
            };
    }

    public sealed class Converter(IDefaultConverter converter) : ElementToExpressionConverter<Condition>
    {
        protected override Expression Build(IBuilderContext context, Condition condition)
        {
            if (condition.IsComparisonOrEqualityOp)
            {
                return BuildComparison(context, condition);
            }

            if (condition.IsMethod)
            {
                //a call that answers no yes-or-no stands in the wrong place: `where[x.Count()]`
                //folds a collection to a number, and a number is no condition
                var call = context.Builder.BuildValue(context, condition.Children);
                if (call.Type == typeof(bool))
                {
                    return call;
                }
            }

            if (!condition.IsFlagCheck)
            {
                throw new NotSupportedConditionException();
            }

            var left = context.ToExpression(condition.Children[0]);
            return left.Type == typeof(bool) ? left : throw new NotSupportedConditionException();
        }

        private Expression BuildComparison(IBuilderContext context, Condition condition)
        {
            var (left, right) = condition.Sides();
            var builder = context.Builder;

            //the side that says what is compared is built first: a value the query wrote becomes
            //whatever the side it stands against answers, so it cannot be built before that
            var comparisonContext = new ArithmeticComparisonConditionBuilderContext(context, converter);
            Expression leftExpression;
            Expression rightExpression;
            if (builder.AnswersWithItsOwnType(left))
            {
                leftExpression = builder.BuildValue(comparisonContext, left);
                comparisonContext.ComparisonType = leftExpression.Type;
                rightExpression = builder.BuildValue(comparisonContext, right);
            }
            else if (builder.AnswersWithItsOwnType(right))
            {
                rightExpression = builder.BuildValue(comparisonContext, right);
                comparisonContext.ComparisonType = rightExpression.Type;
                leftExpression = builder.BuildValue(comparisonContext, left);
            }
            else
            {
                //both sides are written values: no type of the row says what they are, and nothing
                //here reads the CLR type of a constant to guess it
                throw new ConditionElementMustHaveLeftAndRightException();
            }

            var comparison = condition.Comparison?.Comparison ??
                             throw new ComparisonConditionMustHaveComparisonTokenException();

            return builder.BuildComparison(comparison, leftExpression, rightExpression);
        }
    }
}
