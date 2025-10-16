namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class PropertyAccess(Range range) : MemberAccess(range)
{
    public sealed class Possibility() : TokenPossibility<PropertyAccess>(delimiters:
    [
        Dot.Possibility.TokenValue.AsSpan()[0],
        Equality.Possibility.TokenValue.AsSpan()[0],
        Inequality.Possibility.TokenValue.AsSpan()[0],
        LessThan.Possibility.TokenValue.AsSpan()[0],
        LessOrEqualThan.Possibility.TokenValue.AsSpan()[0],
        GreaterThan.Possibility.TokenValue.AsSpan()[0],
        GreaterOrEqualThan.Possibility.TokenValue.AsSpan()[0],
        And.Possibility.TokenValue.AsSpan()[0],
        Or.Possibility.TokenValue.AsSpan()[0],
        RightSquareBracket.Possibility.TokenValue.AsSpan()[0],
        Comma.Possibility.TokenValue.AsSpan()[0]
    ])
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.SelectPreviousTokensMatch<PropertyAccess>(previousTokens);

        protected override PropertyAccess BuildImpl(Range range) => new(range);
    }
}