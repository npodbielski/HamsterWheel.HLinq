namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenBy(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenBy>(grammar, "thenBy")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);

        protected override ThenBy BuildImpl(Range range) => new(range);
    }
}