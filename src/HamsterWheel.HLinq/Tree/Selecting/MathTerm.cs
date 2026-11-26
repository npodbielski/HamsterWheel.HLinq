using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Ordering;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// One operator of a math expression with the value written after it.
/// </summary>
public sealed class MathTerm(IToken[] tokens, IMathOperatorToken op) : TreeBranch(tokens)
{
    public IMathOperatorToken Operator { get; } = op;

    // ReSharper disable once UnusedType.Global -> used by the Parser
    public sealed class Parser : ElementParserBase<MathTerm>
    {
        public override Type[] ValidParents { get; } =
        [
            typeof(PropertyAssignment), typeof(MathGroup), typeof(Condition),
            typeof(OrderByRoot), typeof(OrderByDescendingRoot), typeof(ThenByRoot), typeof(ThenByDescendingRoot)
        ];

        public override IToken[] ExampleTokens { get; } = [Plus.Empty];

        protected override MathTerm? BuildBranch(IParsingContext context) =>
            context.Tokens is [IMathOperatorToken op, ..] ? new MathTerm(context.Tokens[..1], op) : null;
    }
}
