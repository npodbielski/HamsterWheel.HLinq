namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class PropertyAccess(Range range) : MemberAccess(range)
{
    public sealed class Possibility() : TokenPossibility<PropertyAccess>(delimiters:
    [
        Dot.TokenValue.AsSpan()[0],
        Equality.TokenValue.AsSpan()[0],
        Inequality.TokenValue.AsSpan()[0],
        LessThan.TokenValue.AsSpan()[0],
        LessOrEqualThan.TokenValue.AsSpan()[0],
        GreaterThan.TokenValue.AsSpan()[0],
        GreaterOrEqualThan.TokenValue.AsSpan()[0],
        And.TokenValue.AsSpan()[0],
        Or.TokenValue.AsSpan()[0],
        RightSquareBracket.TokenValue.AsSpan()[0],
        Comma.TokenValue.AsSpan()[0]
    ])
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens)
        {
            return previousTokens.Count >= 4 &&
                   previousTokens is [.., Entity, Dot] or [.., PropertyAccess, Dot];
        }

        protected override PropertyAccess BuildImpl(Range range)
        {
            return new PropertyAccess(range);
        }
    }
}