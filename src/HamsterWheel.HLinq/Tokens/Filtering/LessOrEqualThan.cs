namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LessOrEqualThan>(grammar, "<=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<LessOrEqualThan>(previousToken);

        protected override LessOrEqualThan BuildImpl(Range range) => new(range);
    }
}