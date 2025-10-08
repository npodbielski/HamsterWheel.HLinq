using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface IHLinqParsersCollection
{
    IElementParser[] Parsers { get; }
    IHLinqTokenPossibility[] Tokens { get; }
}