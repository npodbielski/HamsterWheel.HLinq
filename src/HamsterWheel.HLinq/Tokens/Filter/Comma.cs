namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class Comma(Range range) : TokenBase(range)
{
    public const string? TokenValue = ",";

    public sealed class Possibility() : TokenPossibility<Comma>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is NameOrValue or PropertyAccess;

        protected override Comma BuildImpl(Range range) => new(range);
    }
}