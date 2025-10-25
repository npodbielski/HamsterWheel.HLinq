using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public class TreeLeaf(IToken[] tokens) : ITreeElement
{
    public IToken[] Tokens { get; } = tokens;
    public bool NoChildren => false;
}