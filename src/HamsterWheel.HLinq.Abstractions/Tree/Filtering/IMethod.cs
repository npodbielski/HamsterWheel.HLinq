using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Filtering;

public interface IMethod : ITreeBranch
{
    string GetName(string hLinqQuery);

    /// <summary>
    /// The path of the property argument (<c>x.Age</c> in <c>x.Max(x.Age)</c>), null when the call
    /// takes none.
    /// </summary>
    string[]? ArgumentPath(string hLinqQuery);
}