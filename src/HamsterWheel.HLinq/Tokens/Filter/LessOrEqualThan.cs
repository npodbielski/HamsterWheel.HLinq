namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class LessOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = "<=";

    public sealed class Possibility() : TokenPossibility<LessOrEqualThan>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is PropertyAccess or RightSquareBracket;

        protected override LessOrEqualThan BuildImpl(Range range) => new(range);
    }
}