namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class Where(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "where";

    public sealed class Possibility() : TokenPossibility<Where>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Where BuildImpl(Range range) => new(range);
    }
}