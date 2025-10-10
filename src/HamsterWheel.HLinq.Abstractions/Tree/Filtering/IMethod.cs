using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Tree.Filtering;

public interface IMethod : ITreeBranch
{
    string GetName(string hLinqQuery);
}