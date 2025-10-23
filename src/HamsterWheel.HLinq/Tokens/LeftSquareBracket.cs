using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<LeftSquareBracket>(TokenValue)
    {
        public const string TokenValue = "[";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
           GrammarRules.PreviousTokenMatch<LeftSquareBracket>(previousToken);

        protected override LeftSquareBracket BuildImpl(Range range) => new(range);
    }
}