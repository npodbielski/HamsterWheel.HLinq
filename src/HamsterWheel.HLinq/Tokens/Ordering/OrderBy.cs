namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderBy(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<OrderBy>(grammar, "orderBy")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);

        protected override OrderBy BuildImpl(Range range) => new(range);
    }
}