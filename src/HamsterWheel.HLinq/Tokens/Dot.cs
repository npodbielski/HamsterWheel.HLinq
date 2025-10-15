using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tokens;

public sealed class Dot(Range range) : TokenBase(range)
{
    public const string TokenValue = ".";

    public sealed class Possibility() : TokenPossibility<Dot>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is Entity or PropertyAccess or RightSquareBracket;

        protected override Dot BuildImpl(Range range) => new(range);
    }
}