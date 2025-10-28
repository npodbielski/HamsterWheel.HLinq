using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class SkipOrTakeConstant(NameOrValue value) : TreeLeaf([value])
{
    public NameOrValue Value => value;

    public sealed class Parser : ElementParserBase<SkipOrTakeConstant>
    {
        public override IToken[] ExampleTokens { get; } = [new TokenExample("10")];
        protected override Type[] ValidParents { get; } = [typeof(SkipRoot), typeof(TakeRoot)];

        protected override SkipOrTakeConstant? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [NameOrValue value, ..] => new SkipOrTakeConstant(value),
                _ => null
            };
    }
}