namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Skip>(grammar, "skip")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override Skip BuildImpl(Range range) => new(range);
    }
}