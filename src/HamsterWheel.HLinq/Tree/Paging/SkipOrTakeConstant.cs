using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class SkipOrTakeConstant(NameOrValue value) : TreeLeaf([value])
{
    public NameOrValue Value => value;

    public sealed class Parser : ElementParserBase<SkipOrTakeConstant>
    {
        protected override Type[] ValidParents => [typeof(SkipRoot), typeof(TakeRoot)];

        protected override SkipOrTakeConstant? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [NameOrValue value, ..] => new SkipOrTakeConstant(value),
                _ => null
            };
        }
    }
}