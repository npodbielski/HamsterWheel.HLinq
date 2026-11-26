using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Selecting;

public interface IInitializerPropertyName : ITreeElement
{
    string GetName(string hLinqQuery);
}