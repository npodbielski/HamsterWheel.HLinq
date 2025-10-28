namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenBase : IToken
{
    public Range Range { get; protected internal set; }

    public string GetValue(string str) => str[Range];
}