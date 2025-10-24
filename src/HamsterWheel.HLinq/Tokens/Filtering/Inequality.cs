namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Inequality(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Inequality>(grammar, "!=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override Inequality BuildImpl(Range range) => new(range);
    }
}