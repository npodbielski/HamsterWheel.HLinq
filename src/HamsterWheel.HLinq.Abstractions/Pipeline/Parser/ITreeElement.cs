using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public interface ITreeElement
{
    IToken[] Tokens { get; }
    bool NoChildren { get; }
}