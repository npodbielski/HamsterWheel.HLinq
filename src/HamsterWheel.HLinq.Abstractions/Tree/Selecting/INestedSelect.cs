using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Selecting;

public interface INestedSelect : ITreeBranch
{
    IReadOnlyList<IPropertyAssignment> Members { get; }
}