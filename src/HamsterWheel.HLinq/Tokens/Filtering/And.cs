namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And(Range range) : TokenBase(range), ILogicalOperatorToken
{
    public const string TokenValue = "&&";

    public sealed class Possibility() : TokenPossibility<And>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is NameOrValue or PropertyAccess or RightCircleBracket;

        protected override And BuildImpl(Range range) => new(range);
    }
}