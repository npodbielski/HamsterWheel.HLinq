namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderByDescending(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<OrderByDescending>(TokenValue)
    {
        public const string TokenValue = "orderByDescending";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.SelectPreviousTokensMatch<OrderByDescending>(previousTokens);

        protected override OrderByDescending BuildImpl(Range range) => new(range);
    }
}