namespace HamsterWheel.HLinq.Tokens;

public interface IHLinqTokenPossibility
{
    int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousTokens);

    IToken Build(Range range);
}