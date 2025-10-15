namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderByDescending(Range range) : TokenBase(range)
{
    public const string TokenValue = "orderByDescending";

    public sealed class Possibility() : TokenPossibility<OrderByDescending>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override OrderByDescending BuildImpl(Range range) => new(range);
    }
}