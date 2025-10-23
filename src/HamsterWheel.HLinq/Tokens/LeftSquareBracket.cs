namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LeftSquareBracket>(grammar, TokenValue)
    {
        public const string TokenValue = "[";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
           Grammar.PreviousTokenMatch<LeftSquareBracket>(previousToken);

        protected override LeftSquareBracket BuildImpl(Range range) => new(range);
    }
}