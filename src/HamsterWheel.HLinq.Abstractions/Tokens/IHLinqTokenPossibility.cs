namespace HamsterWheel.HLinq.Tokens;

public interface IHLinqTokenPossibility
{
    bool CanBeFirst { get; }
    int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousTokens);

    IToken Build(Range range);
}