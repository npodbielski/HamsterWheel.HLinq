namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<GreaterOrEqualThan>(grammar, ">=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<GreaterOrEqualThan>(previousToken);

        protected override GreaterOrEqualThan BuildImpl(Range range) => new(range);
    }
}