namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where(Range range) : TokenBase(range)
{
    public const string TokenValue = "where";

    public sealed class Possibility() : TokenPossibility<Where>(TokenValue)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Where BuildImpl(Range range) => new(range);
    }
}