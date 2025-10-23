namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderBy(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<OrderBy>(grammar, "orderBy")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<OrderBy>(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next is not null &&
            char.ToLower(next.Value).Equals(char.ToLower(LeftSquareBracket.Possibility.TokenValue.AsSpan()[0]));

        protected override OrderBy BuildImpl(Range range) => new(range);
    }
}