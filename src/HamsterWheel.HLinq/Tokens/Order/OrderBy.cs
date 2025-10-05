namespace HamsterWheel.HLinq.Tokens.Order;

public sealed class OrderBy(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "orderBy";

    public sealed class Possibility() : TokenPossibility<OrderBy>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next is not null &&
            char.ToLower(next.Value).Equals(char.ToLower(LeftSquareBracket.TokenValue.AsSpan()[0]));

        protected override OrderBy BuildImpl(Range range) => new(range);
    }
}