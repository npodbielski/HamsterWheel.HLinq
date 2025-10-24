namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenByDescending : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenByDescending>(grammar, "thenByDescending")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static ThenByDescending Build(Range range) => new() { Range = range };
    public static ThenByDescending Empty { get; } = new() { Range = default };
}