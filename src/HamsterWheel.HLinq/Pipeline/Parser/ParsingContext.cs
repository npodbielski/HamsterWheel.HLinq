using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

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

    public void GoToParent() => Current = Parents.Pop();

    public void RemoveStartTokens(int number) => Tokens = Tokens[number..];
}