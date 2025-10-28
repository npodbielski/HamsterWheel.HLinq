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
    public void Push(ITreeElement newElement);
    void GoToParent();
    public void RemoveStartTokens(int number);
}