using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface IParsingContext
{
    public IGrowingElementContext Current { get; }
    public ITreeElement CurrentElement { get; }
    public ITreeBranch? CurrentBranch { get; }
    public IToken[] Tokens { get; }

    public Stack<IGrowingElementContext> Parents { get; set; }
    List<ITreeElement> Children { get; }

    public void Push(ITreeElement newElement);
    void GoBackInTheTree();

    public void RemoveTokensFromStart(int number);
}