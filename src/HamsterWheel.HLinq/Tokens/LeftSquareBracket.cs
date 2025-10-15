using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket(Range range) : TokenBase(range)
{
    public const string TokenValue = "[";

    public sealed class Possibility() : TokenPossibility<LeftSquareBracket>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is Where or Skip or Take or OrderBy or OrderByDescending or ThenBy or ThenByDescending
                or Select or Count;

        protected override LeftSquareBracket BuildImpl(Range range) => new(range);
    }
}