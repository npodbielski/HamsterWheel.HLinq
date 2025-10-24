namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Take>(grammar, "take")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override Take BuildImpl(Range range) => new(range);
    }
}