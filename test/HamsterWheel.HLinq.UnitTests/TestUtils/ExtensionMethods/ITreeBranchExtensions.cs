using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.UnitTests.TestUtils;

public static class ITreeBranchExtensions
{
    public static IEnumerable<T> GetAll<T>(this ITreeBranch element) where T : ITreeElement =>
        element.Children.SelectMany(b => b switch
        {
            T item => [item],
            TreeBranch branch => branch.GetAll<T>(),
            _ => []
        });
}