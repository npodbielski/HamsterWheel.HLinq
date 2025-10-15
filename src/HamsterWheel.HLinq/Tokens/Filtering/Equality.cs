namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Equality(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = "==";

    public sealed class Possibility() : TokenPossibility<Equality>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is PropertyAccess or RightSquareBracket;

        protected override Equality BuildImpl(Range range) => new(range);
    }
}