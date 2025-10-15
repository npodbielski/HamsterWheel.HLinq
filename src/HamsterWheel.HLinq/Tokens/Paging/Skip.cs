namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip(Range range) : TokenBase(range)
{
    public const string TokenValue = "skip";

    public sealed class Possibility() : TokenPossibility<Skip>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Skip BuildImpl(Range range) => new(range);
    }
}