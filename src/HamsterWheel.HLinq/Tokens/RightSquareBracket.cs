namespace HamsterWheel.HLinq.Tokens;

public sealed class RightSquareBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RightSquareBracket>(grammar, TokenValue)
    {
        public const string TokenValue = "]";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<RightSquareBracket>(previousToken);

        protected override RightSquareBracket BuildImpl(Range range) => new(range);
    }
}