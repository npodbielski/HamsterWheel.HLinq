using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public sealed class ParsingContext(IHLinqQuery root) : IParsingContext
{
    public IGrowingElementContext Current { get; private set; } = new GrowingElementContext(root, []);
    public ITreeElement CurrentElement => Current.Element;
    public ITreeBranch? CurrentBranch => CurrentElement as ITreeBranch;
    public List<ITreeElement> Children => Current.Children;
    public IToken[] Tokens { get; set; } = [];
    public Stack<IGrowingElementContext> Parents { get; set; } = [];
    public string SourceQueryString { get; } = root.SourceQueryString;

    public void Push(ITreeElement newElement)
    {
        Current.Children.Add(newElement);
        Parents.Push(Current);
        Current = new GrowingElementContext(newElement, []);
    }

    public void GoBackInTheTree() => Current = Parents.Pop();

    public void RemoveTokensFromStart(int number) => Tokens = Tokens[number..];
}