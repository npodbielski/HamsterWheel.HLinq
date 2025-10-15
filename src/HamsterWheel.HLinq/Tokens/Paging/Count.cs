namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Count(Range range) : TokenBase(range)
{
    public const string TokenValue = "count";

    public sealed class Possibility() : TokenPossibility<Count>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Count BuildImpl(Range range) => new(range);
    }
}