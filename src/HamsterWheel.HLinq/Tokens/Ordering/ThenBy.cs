namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenBy(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<ThenBy>(TokenValue)
    {
        public const string TokenValue = "thenBy";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.PreviousTokensMatch<ThenBy>(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            GrammarRules.NextCharIsAllowed<ThenBy>(next);

        protected override ThenBy BuildImpl(Range range) => new(range);
    }
}