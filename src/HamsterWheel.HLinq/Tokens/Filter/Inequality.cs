namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class Inequality(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = "!=";

    public sealed class Possibility() : TokenPossibility<Inequality>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is PropertyAccess or NameOrValue;

        protected override Inequality BuildImpl(Range range) => new(range);
    }
}