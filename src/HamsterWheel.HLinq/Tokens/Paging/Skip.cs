namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Skip>(TokenValue)
    {
        public const string TokenValue = "skip";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.SelectPreviousTokensMatch<Skip>(previousTokens);

        protected override Skip BuildImpl(Range range) => new(range);
    }
}