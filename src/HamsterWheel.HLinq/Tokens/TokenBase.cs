namespace HamsterWheel.HLinq.Tokens;

public abstract partial class TokenBase : IToken
{
    public Range Range { get; protected internal set; }
}