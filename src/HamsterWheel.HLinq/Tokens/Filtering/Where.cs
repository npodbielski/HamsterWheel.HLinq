namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Where>(TokenValue)
    {
        public const string TokenValue = "where";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.PreviousTokensMatch<Where>(previousTokens);

        protected override Where BuildImpl(Range range) => new(range);
    }
}