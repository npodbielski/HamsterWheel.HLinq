namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Comma(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Comma>(TokenValue)
    {
        public const string TokenValue = ",";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.SelectPreviousTokenMatch<Comma>(previousToken);

        protected override Comma BuildImpl(Range range) => new(range);
    }
}