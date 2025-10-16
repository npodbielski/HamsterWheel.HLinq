namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Inequality(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility() : TokenPossibility<Inequality>(TokenValue)
    {
        public const string TokenValue = "!=";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.SelectPreviousTokenMatch<Inequality>(previousToken);

        protected override Inequality BuildImpl(Range range) => new(range);
    }
}