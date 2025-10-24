namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenByDescending(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenByDescending>(grammar, "thenByDescending")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override ThenByDescending BuildImpl(Range range) => new(range);
    }
}