using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Paging;

partial class TakeRoot
{
    private sealed class SkipOrTakeConstantTokenMissingException()
        : HLinqQueryException("Take query method needs to have number parameter");
}