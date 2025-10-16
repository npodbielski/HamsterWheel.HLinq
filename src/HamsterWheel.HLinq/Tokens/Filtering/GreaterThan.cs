namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility() : TokenPossibility<GreaterThan>(TokenValue)
    {
        public const string TokenValue = ">";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.SelectPreviousTokenMatch<GreaterThan>(previousToken);

        protected override GreaterThan BuildImpl(Range range) => new(range);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            GrammarRules.NextCharIsAllowed<GreaterThan>(next);
    }
}