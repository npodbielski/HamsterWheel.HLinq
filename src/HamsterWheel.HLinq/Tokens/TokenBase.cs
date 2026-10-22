namespace HamsterWheel.HLinq.Tokens;

public abstract partial class TokenBase : IToken
{
    public Range Range { get; protected init; }

    public static T Create<T>(Range range) where T : TokenBase, new() => new() { Range = range };
}