namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Equality(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Equality>(grammar, "==")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override Equality BuildImpl(Range range) => new(range);
    }
}