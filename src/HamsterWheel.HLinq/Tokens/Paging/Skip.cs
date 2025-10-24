namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Skip>(grammar, "skip")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static Skip Build(Range range) => new() { Range = range };
    public static Skip Empty { get; } = new() { Range = default };
}