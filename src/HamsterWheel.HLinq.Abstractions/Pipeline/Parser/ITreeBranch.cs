using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public interface ITreeBranch : ITreeElement
{
    ITreeElement[] Children { get; }
    void Finish(IParsingContext context, IToken[] endingTokens);
}