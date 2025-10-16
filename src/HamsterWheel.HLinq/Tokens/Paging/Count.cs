namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Count(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Count>(TokenValue)
    {
        public const string TokenValue = "count";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.SelectPreviousTokensMatch<Count>(previousTokens);

        protected override Count BuildImpl(Range range) => new(range);
    }
}