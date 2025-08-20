namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "take";

    public sealed class Possibility() : TokenPossibility<Take>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Take BuildImpl(Range range) => new(range);
    }
}