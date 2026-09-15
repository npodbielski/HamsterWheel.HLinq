using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class SkipOrTakeConstant(NameOrValue value) : TreeLeaf([value])
{
    public NameOrValue Value => value;

    public sealed class Parser : ElementParserBase<SkipOrTakeConstant>
    {
        public override IToken[] ExampleTokens { get; } = [new TokenExample("10")];
        protected override Type[] ValidParents { get; } = [typeof(SkipRoot), typeof(TakeRoot), typeof(SkipOrTakeRange)];

        protected override SkipOrTakeConstant? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                // start side of a take[start..end] range is claimed by SkipOrTakeRange.Parser
                [NameOrValue, RangeOperator, ..] => null,
                [NameOrValue value, ..] => new SkipOrTakeConstant(value),
                _ => null
            };
    }
}