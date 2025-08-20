using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public abstract class TreeBranch(IToken[] startingTokens) : ITreeBranch
{
    private bool _finished;
    public virtual bool NoChildren => false;
    public bool IsBranch => true;
    public bool IsLeaf => false;
    public ITreeElement[] Children { get; private set; } = [];
    public IToken[] Tokens { get; private set; } = startingTokens;

    public bool Finished => _finished && Children.All(b => b.Finished);

    public void Finish(IParsingContext context, IToken[] endingTokens)
    {
        _finished = true;
        if (context.Children.Count != 0) Children = context.Children.ToArray();

        AddEndingTokens(endingTokens);
    }

    public IEnumerable<T> GetAll<T>() where T : ITreeElement
    {
        return Children.SelectMany(b => b.GetAll<T>());
    }

    protected virtual void AddEndingTokens(IToken[] endingTokens)
    {
        if (endingTokens.Length != 0) Tokens = Tokens.Concat(endingTokens).ToArray();
    }

    public T? GetChildOfType<T>() where T : ITreeElement
    {
        return Children.OfType<T>().SingleOrDefault();
    }
}