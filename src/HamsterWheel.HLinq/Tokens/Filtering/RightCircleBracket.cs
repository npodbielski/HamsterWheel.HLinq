namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class RightCircleBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RightCircleBracket>(grammar, TokenValue)
    {
        public const string TokenValue = ")";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<RightCircleBracket>(previousToken);

        protected override RightCircleBracket BuildImpl(Range range) => new(range);
    }
}