namespace HamsterWheel.HLinq.Tokens.Order;

public sealed class ThenByDescending(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "thenByDescending";

    public sealed class Possibility() : TokenPossibility<ThenByDescending>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override ThenByDescending BuildImpl(Range range) => new(range);
    }
}