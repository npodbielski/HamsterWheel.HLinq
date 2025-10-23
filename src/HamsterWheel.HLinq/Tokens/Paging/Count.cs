namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Count(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Count>(grammar, TokenValue)
    {
        public const string TokenValue = "count";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<Count>(previousTokens);

        protected override Count BuildImpl(Range range) => new(range);
    }
}