using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public interface IHLinqTokenPossibility
{
    bool CanBeFirst { get; }
    Type ForType { get; }
    string? Keyword { get; }
    int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousTokens);
    IToken Build(Range range);
}