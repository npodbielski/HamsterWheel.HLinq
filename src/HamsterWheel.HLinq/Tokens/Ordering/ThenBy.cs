namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenBy : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenBy>(grammar, "thenBy")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    public static ThenBy Build(Range range) => new() { Range = range };
    public static ThenBy Empty { get; } = new() { Range = default };
}