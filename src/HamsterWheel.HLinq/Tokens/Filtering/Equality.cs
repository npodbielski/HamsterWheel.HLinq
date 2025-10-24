namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Equality : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Equality>(grammar, "==")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static Equality Build(Range range) => new() { Range = range };
    public static Equality Empty { get; } = new() { Range = default };
}