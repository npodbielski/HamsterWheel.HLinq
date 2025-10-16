namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Where>(TokenValue)
    {
        public const string TokenValue = "where";
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Where BuildImpl(Range range) => new(range);
    }
}