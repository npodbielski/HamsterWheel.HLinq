using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public class TreeLeaf(IToken[] tokens) : ITreeElement
{
    public bool IsBranch => false;
    public bool IsLeaf => true;
    public IToken[] Tokens { get; } = tokens;
    public bool Finished => true;
    public bool NoChildren => false;

    //TODO: move this to another interface that is implemented only in TreeBranch
    public void Finish(IParsingContext context, IToken[] tokens1)
    {
    }

    public IEnumerable<T> GetAll<T>() where T : ITreeElement
    {
        if (this is T value) yield return value;
    }
}