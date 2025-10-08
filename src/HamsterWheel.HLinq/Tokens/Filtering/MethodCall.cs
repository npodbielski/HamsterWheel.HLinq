namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class MethodCall(Range range) : MemberAccess(range)
{
    public sealed class Possibility()
        : TokenPossibility<MethodCall>(delimiters: [LeftCircleBracket.TokenValue.AsSpan()[0]])
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is Dot or LeftSquareBracket;

        protected override MethodCall BuildImpl(Range range) => new(range);
    }
}