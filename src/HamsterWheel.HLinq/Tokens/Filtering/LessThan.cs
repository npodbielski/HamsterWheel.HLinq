namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessThan : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LessThan>(grammar, "<")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    public static LessThan Build(Range range) => new() { Range = range };
    public static LessThan Empty { get; } = new() { Range = default };
}