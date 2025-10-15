using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public class TreeLeaf(IToken[] tokens) : ITreeElement
{
    public IToken[] Tokens { get; } = tokens;
    public bool NoChildren => false;
}