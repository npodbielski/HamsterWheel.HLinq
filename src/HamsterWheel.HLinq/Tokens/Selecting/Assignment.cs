namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Assignment>(TokenValue)
    {
        public const string TokenValue = "=";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.PreviousTokenMatch<Assignment>(previousToken);

        protected override Assignment BuildImpl(Range range) => new(range);
    }
}