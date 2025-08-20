namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenBase(Range range) : IToken
{
    public Range Range { get; } = range;
    public IToken Token => this;

    public string GetValue(string str) => str[Range];
}