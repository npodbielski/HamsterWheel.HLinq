using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree;

public interface IProperty : IMethodParamElement, ITreeElement
{
    string[] GetPath(string hLinqQuery);

    /// <summary>
    /// The last segment of the path, read from the query text without building all of them.
    /// </summary>
    string GetLastName(string hLinqQuery);
}