using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Selecting;

public interface IInnerProperty : ITreeElement
{
    string[] GetPath(string hLinqQuery);
    string GetLastName(string hLinqQuery);
}