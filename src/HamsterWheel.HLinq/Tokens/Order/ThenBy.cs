namespace HamsterWheel.HLinq.Tokens.Order;

public sealed class ThenBy(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "thenBy";

    public sealed class Possibility() : TokenPossibility<ThenBy>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens)
        {
            return previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];
        }

        protected override bool NextIsAllowedWhenKeywordMatch(char? next)
        {
            return next == LeftSquareBracket.TokenValue.AsSpan()[0];
        }

        protected override ThenBy BuildImpl(Range range)
        {
            return new ThenBy(range);
        }
    }
}