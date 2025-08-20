namespace HamsterWheel.HLinq.Tokens.Order;

public sealed class OrderByDescending(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "orderByDescending";

    public sealed class Possibility() : TokenPossibility<OrderByDescending>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens)
        {
            return previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];
        }

        protected override OrderByDescending BuildImpl(Range range)
        {
            return new OrderByDescending(range);
        }
    }
}