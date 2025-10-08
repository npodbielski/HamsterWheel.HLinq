namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderBy(Range range) : TokenBase(range)
{
    public const string TokenValue = "orderBy";

    public sealed class Possibility() : TokenPossibility<OrderBy>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next is not null &&
            char.ToLower(next.Value).Equals(char.ToLower(LeftSquareBracket.TokenValue.AsSpan()[0]));

        protected override OrderBy BuildImpl(Range range) => new(range);
    }
}