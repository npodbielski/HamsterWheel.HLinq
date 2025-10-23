namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderByDescending(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<OrderByDescending>(grammar, "orderByDescending")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<OrderByDescending>(previousTokens);

        protected override OrderByDescending BuildImpl(Range range) => new(range);
    }
}