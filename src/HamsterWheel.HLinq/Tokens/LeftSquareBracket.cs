using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tokens.Order;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket(Range range) : TokenBase(range)
{
    public const string TokenValue = "[";

    public sealed class Possibility() : TokenPossibility<LeftSquareBracket>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is Where or Skip or Take or OrderBy or OrderByDescending or ThenBy or ThenByDescending
                or Select.Select or Count;

        protected override LeftSquareBracket BuildImpl(Range range) => new(range);
    }
}