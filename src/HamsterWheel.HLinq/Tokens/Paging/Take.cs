namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Take>(grammar, "take")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static Take Build(Range range) => new() { Range = range };
    public static Take Empty { get; } = new() { Range = default };
}