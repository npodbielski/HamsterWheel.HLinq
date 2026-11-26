using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public interface IParsingContext
{
    public IGrowingElementContext Current { get; }
    public ITreeElement CurrentElement { get; }
    public ITreeBranch? CurrentBranch { get; }
    public IToken[] Tokens { get; }
    public Stack<IGrowingElementContext> Parents { get; set; }
    List<ITreeElement> Children { get; }
    string SourceQueryString { get; }

    /// <summary>
    /// Whether a <c>groupBy</c> has been committed to the query so far.
    /// </summary>
    public bool GroupingApplied { get; set; }

    /// <summary>
    /// Checks whether the clause finished here names a member only a grouping has.
    /// </summary>
    public void CheckAfterGroup();

    public void Push(ITreeElement newElement);
    void GoToParent();
    public void RemoveStartTokens(int number);
}