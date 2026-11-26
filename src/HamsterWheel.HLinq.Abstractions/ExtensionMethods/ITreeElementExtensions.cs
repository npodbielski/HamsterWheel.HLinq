using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq;

public static class ITreeElementExtensions
{
    public static T GetFirstTokenOf<T>(this ITreeElement element) where T : IToken =>
        element.Tokens.OfType<T>().First();

    public static IEnumerable<T> GetTokensOf<T>(this ITreeElement element) where T : IToken =>
        element.Tokens.OfType<T>();

    public static IEnumerable<T> GetTokensOfNot<T>(this ITreeElement element) where T : IToken =>
        element.Tokens.Where(t => t is not T).Cast<T>();

    /// <summary>
    /// Every token of this element and its subtree, in the order the query wrote them.
    /// </summary>
    public static IEnumerable<IToken> GetTokensOfSubtree(
        this ITreeElement element,
        Func<IToken, bool> acceptToken,
        Func<ITreeElement, bool>? acceptChild = null) =>
        SubtreeOf(element, acceptToken, acceptChild ?? (_ => true));

    /// <summary>
    /// Number of properties of the widest member list of an element, nested lists counted as their own.
    /// </summary>
    public static int MaxNumberOfProperties(this ITreeElement element) => ListsOf(element).Max(NumberOfProperties);

    private static IEnumerable<IToken> SubtreeOf(
        ITreeElement element,
        Func<IToken, bool> acceptToken,
        Func<ITreeElement, bool> acceptChild)
    {
        foreach (var token in element.Tokens.Where(acceptToken))
        {
            yield return token;
        }

        if (element is not ITreeBranch branch)
        {
            yield break;
        }

        foreach (var child in branch.Children.Where(acceptChild))
        {
            foreach (var deeper in SubtreeOf(child, acceptToken, acceptChild))
            {
                yield return deeper;
            }
        }
    }

    private static IEnumerable<ITreeElement> ListsOf(ITreeElement element)
    {
        yield return element;

        foreach (var child in element is ITreeBranch branch ? branch.Children : [])
        {
            foreach (var deeper in ListsOf(child))
            {
                yield return deeper;
            }
        }
    }

    private static int NumberOfProperties(ITreeElement element) => element switch
    {
        //a member a nested select writes without naming it is a path and not an assignment - both
        //of them are one column of the answer, and the cap counts columns
        INestedSelect nested => nested.Members.Count +
                                nested.Children.OfType<IProperty>().Count(),
        IProjectionMembers list => list.Children.OfType<IPropertyAssignment>().Count(),
        _ => 0,
    };
}
