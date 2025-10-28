using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree;

public interface ILogicalOperationGroupBranch : ITreeBranch
{
    ILogicalOperatorToken? LogicalOpToken { get; }
}