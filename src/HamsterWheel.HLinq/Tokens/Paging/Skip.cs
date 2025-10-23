namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Skip>(grammar, TokenValue)
    {
        public const string TokenValue = "skip";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<Skip>(previousTokens);

        protected override Skip BuildImpl(Range range) => new(range);
    }
}