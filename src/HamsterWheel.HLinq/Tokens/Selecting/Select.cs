namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Select(Range range) : TokenBase(range)
{
    public const string TokenValue = "select";

    public sealed class Possibility() : TokenPossibility<Select>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) => previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Select BuildImpl(Range range) => new(range);
    }
}