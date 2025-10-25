using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public interface IHLinqParser
{
    IHLinqQuery Parse(IHLinqQuery query, IToken[] tokens);
}