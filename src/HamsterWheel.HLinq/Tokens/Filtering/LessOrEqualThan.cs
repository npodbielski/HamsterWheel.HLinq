namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessOrEqualThan : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LessOrEqualThan>(grammar, "<=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static LessOrEqualThan Build(Range range) => new() { Range = range };
    public static LessOrEqualThan Empty { get; } = new() { Range = default };
}