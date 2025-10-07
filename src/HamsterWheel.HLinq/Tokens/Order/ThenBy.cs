namespace HamsterWheel.HLinq.Tokens.Order;

public sealed class ThenBy(Range range) : TokenBase(range)
{
    public const string TokenValue = "thenBy";

    public sealed class Possibility() : TokenPossibility<ThenBy>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next == LeftSquareBracket.TokenValue.AsSpan()[0];

        protected override ThenBy BuildImpl(Range range) => new(range);
    }
}