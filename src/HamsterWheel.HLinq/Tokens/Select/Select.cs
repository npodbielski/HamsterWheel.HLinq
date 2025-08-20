namespace HamsterWheel.HLinq.Tokens.Select;

public sealed class Select(Range range) : TokenBase(range)
{
    public const string HLinqQueryToken = "select";

    public sealed class Possibility() : TokenPossibility<Select>(HLinqQueryToken)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) => previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];

        protected override Select BuildImpl(Range range) => new(range);
    }
}