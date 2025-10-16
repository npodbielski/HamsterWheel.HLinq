namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Take>(TokenValue)
    {
        public const string TokenValue = "take";
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.SelectPreviousTokensMatch<Take>(previousTokens);

        protected override Take BuildImpl(Range range) => new(range);
    }
}