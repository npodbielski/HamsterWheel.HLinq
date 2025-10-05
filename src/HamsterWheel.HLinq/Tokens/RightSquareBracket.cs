using HamsterWheel.HLinq.Tokens.Filter;

namespace HamsterWheel.HLinq.Tokens;

public sealed class RightSquareBracket(Range range) : TokenBase(range)
{
    public const string TokenValue = "]";

    public sealed class Possibility() : TokenPossibility<RightSquareBracket>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is PropertyAccess or NameOrValue or RightCircleBracket
                or LeftSquareBracket;

        protected override RightSquareBracket BuildImpl(Range range) => new(range);
    }
}