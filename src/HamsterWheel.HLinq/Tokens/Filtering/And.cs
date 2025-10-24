namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And : TokenBase, ILogicalOperatorToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<And>(grammar, "&&")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static And Build(Range range) => new() { Range = range };
    public static And Empty { get; } = new() { Range = default };
}