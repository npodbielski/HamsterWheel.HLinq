namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenBase(Range range) : IToken
{
    public Range Range { get; } = range;

    public string GetValue(string str) => str[Range];
}