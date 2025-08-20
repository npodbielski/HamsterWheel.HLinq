using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Tree;

public interface ILogicalOperationGroupBranch : ITreeBranch
{
    IConditionalLogicalOperationToken? ConditionalLogicalOp { get; }
}