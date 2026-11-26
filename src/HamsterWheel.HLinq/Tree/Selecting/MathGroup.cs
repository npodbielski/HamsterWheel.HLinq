using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Ordering;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// A math expression written between parentheses, computed before it is used: <c>(x.A+x.B)*2</c>.
/// </summary>
public sealed class MathGroup(IToken[] tokens) : TreeBranch(tokens)
{
    // ReSharper disable once UnusedType.Global -> used by the Parser
    public sealed class Parser : ElementParserBase<MathGroup>
    {
        public override Type[] ValidParents { get; } =
        [
            typeof(MathTerm), typeof(MathGroup), typeof(PropertyAssignment), typeof(Condition),
            typeof(OrderByRoot), typeof(OrderByDescendingRoot), typeof(ThenByRoot), typeof(ThenByDescendingRoot)
        ];

        public override IToken[] ExampleTokens { get; } =
            [LeftCircleBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty, RightCircleBracket.Empty];

        protected override MathGroup? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Assignment, LeftCircleBracket, ..] when context.CurrentBranch is PropertyAssignment =>
                    new MathGroup(context.Tokens[..2]),
                [LeftCircleBracket, ..]
                    when context.CurrentBranch is PropertyAssignment or MathTerm or MathGroup
                         || context.CurrentBranch is OrderByRoot or OrderByDescendingRoot
                             or ThenByRoot or ThenByDescendingRoot
                         || context.Children.LastOrDefault() is ComparisonOperation or MathTerm or MathGroup
                    => new MathGroup(context.Tokens[..1]),
                _ => null
            };

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Children.Count == 0)
            {
                throw new MathGroupHoldsNothingException(context.SourceQueryString);
            }

            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightCircleBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }
    }

    // ReSharper disable once UnusedType.Global -> used by the Applier
    public sealed class Converter : ElementToExpressionConverter<MathGroup>
    {
        protected override Expression Build(IBuilderContext context, MathGroup element) =>
            context.Builder.FoldMath(context, element.Children);
    }

    private sealed class MathGroupHoldsNothingException(string query)
        : HLinqQueryException($"The group in '{query}' holds nothing: write an expression between " +
                              "the parentheses.");
}
