using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface IHLinqParser
{
    IHLinqQuery Parse<T>(IToken[] tokens, string query);
}