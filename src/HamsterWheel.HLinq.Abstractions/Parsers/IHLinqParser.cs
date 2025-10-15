using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface IHLinqParser
{
    IHLinqQuery Parse(IHLinqQuery query, IToken[] tokens);
}