using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface ITreeElement
{
    IToken[] Tokens { get; }
    bool NoChildren { get; }
}