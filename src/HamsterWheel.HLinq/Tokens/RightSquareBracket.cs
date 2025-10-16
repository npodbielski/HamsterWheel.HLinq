using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tokens;

public sealed class RightSquareBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<RightSquareBracket>(TokenValue)
    {
        public const string TokenValue = "]";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.SelectPreviousTokenMatch<RightSquareBracket>(previousToken);

        protected override RightSquareBracket BuildImpl(Range range) => new(range);
    }
}