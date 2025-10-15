namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take(Range range) : TokenBase(range)
{
    public const string TokenValue = "take";

    public sealed class Possibility() : TokenPossibility<Take>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Take BuildImpl(Range range) => new(range);
    }
}