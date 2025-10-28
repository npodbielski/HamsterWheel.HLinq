using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Paging;

partial class SkipRoot
{
    public sealed class MissingSkipValueException()
        : HLinqQueryException("'skip[]' HLinq operation needs to have parameter provided");
}