namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Equality(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility() : TokenPossibility<Equality>(TokenValue)
    {
        public const string TokenValue = "==";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.PreviousTokenMatch<Equality>(previousToken);

        protected override Equality BuildImpl(Range range) => new(range);
    }
}