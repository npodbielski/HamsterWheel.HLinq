using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree;

namespace HamsterWheel.HLinq;

public static class TreeBranchExtensions
{
    /// <summary>
    /// Returns every <see cref="Property" /> in the tree.
    /// </summary>
    public static IEnumerable<IProperty> Properties(this ITreeBranch branch)
    {
        foreach (var child in branch.Children)
        {
            switch (child)
            {
                case IProperty property:
                    yield return property;
                    break;
                case ITreeBranch nested:
                    foreach (var deeper in nested.Properties())
                    {
                        yield return deeper;
                    }

                    break;
            }
        }
    }
}
