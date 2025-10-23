namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Where>(grammar, TokenValue)
    {
        public const string TokenValue = "where";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<Where>(previousTokens);

        protected override Where BuildImpl(Range range) => new(range);
    }
}