namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = ">=";

    public sealed class Possibility() : TokenPossibility<GreaterOrEqualThan>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is PropertyAccess or RightSquareBracket;

        protected override GreaterOrEqualThan BuildImpl(Range range) => new(range);
    }
}