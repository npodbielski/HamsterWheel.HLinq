namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LeftCircleBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<LeftCircleBracket>(TokenValue)
    {
        public const string TokenValue = "(";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.PreviousTokenMatch<LeftCircleBracket>(previousToken);

        protected override LeftCircleBracket BuildImpl(Range range) => new(range);
    }
}