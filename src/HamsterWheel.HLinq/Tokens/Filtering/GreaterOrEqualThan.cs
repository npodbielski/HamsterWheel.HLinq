namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterOrEqualThan : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<GreaterOrEqualThan>(grammar, ">=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static GreaterOrEqualThan Build(Range range) => new() { Range = range };
    public static GreaterOrEqualThan Empty { get; } = new() { Range = default };
}