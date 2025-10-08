using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class ComparisonOperation(IComparisonToken comparison) : TreeLeaf([(TokenBase)comparison])
{
    public IComparisonToken Comparison => comparison;

    public sealed class Parser : ElementParserBase<ComparisonOperation>
    {
        protected override Type[] ValidParents => [typeof(Condition)];

        protected override ComparisonOperation? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [IComparisonToken comparison, ..] => new ComparisonOperation(comparison),
                _ => null
            };
    }
}