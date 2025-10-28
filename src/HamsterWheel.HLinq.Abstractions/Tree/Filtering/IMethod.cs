using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Filtering;

public interface IMethod : ITreeBranch
{
    string GetName(string hLinqQuery);
}