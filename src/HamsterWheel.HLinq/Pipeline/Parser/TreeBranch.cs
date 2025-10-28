using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public abstract class TreeBranch(IToken[] startingTokens) : ITreeBranch
{
    public virtual bool NoChildren => false;
    public ITreeElement[] Children { get; private set; } = [];
    public IToken[] Tokens { get; private set; } = startingTokens;

    public void Finish(IParsingContext context, IToken[] endingTokens)
    {
        if (context.Children.Count != 0)
        {
            Children = context.Children.ToArray();
        }

        AddEndingTokens(endingTokens);
    }

    private void AddEndingTokens(IToken[] endingTokens)
    {
        if (endingTokens.Length != 0)
        {
            Tokens = Tokens.Concat(endingTokens).ToArray();
        }
    }

    protected T? GetChildOfType<T>() where T : ITreeElement => Children.OfType<T>().SingleOrDefault();
}