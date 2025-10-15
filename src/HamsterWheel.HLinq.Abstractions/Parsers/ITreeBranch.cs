using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface ITreeBranch : ITreeElement
{
    public ITreeElement[] Children { get; }
    void Finish(IParsingContext context, IToken[] endingTokens);
}