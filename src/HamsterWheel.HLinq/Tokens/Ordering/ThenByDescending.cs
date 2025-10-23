namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenByDescending(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<ThenByDescending>(TokenValue)
    {
        public const string TokenValue = "thenByDescending";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.PreviousTokensMatch<ThenByDescending>(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            GrammarRules.NextCharIsAllowed<ThenByDescending>(next);

        protected override ThenByDescending BuildImpl(Range range) => new(range);
    }
}