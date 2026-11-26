using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class ComparisonOperation(IComparisonToken comparison) : TreeLeaf([(TokenBase)comparison])
{
    public IComparisonToken Comparison => comparison;

    // ReSharper disable once UnusedType.Global -> used by Parser
    public sealed class Parser : ElementParserBase<ComparisonOperation>
    {
        public override Type[] ValidParents { get; } = [typeof(Condition)];
        public override IToken[] ExampleTokens { get; } = [Equality.Empty];

        protected override ComparisonOperation? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [IComparisonToken comparison, ..] => new ComparisonOperation(comparison),
                _ => null
            };
    }
}