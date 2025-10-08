namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class RightCircleBracket(Range range) : TokenBase(range)
{
    public const string TokenValue = ")";

    public sealed class Possibility() : TokenPossibility<RightCircleBracket>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) => previousToken is PropertyAccess or NameOrValue or RightCircleBracket;

        protected override RightCircleBracket BuildImpl(Range range) => new(range);
    }
}