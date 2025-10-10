using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree;

public interface ILogicalOperationGroupBranch : ITreeBranch
{
    ILogicalOperatorToken? LogicalOpToken { get; }
}