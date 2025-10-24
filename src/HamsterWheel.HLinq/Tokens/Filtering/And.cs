namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And(Range range) : TokenBase(range), ILogicalOperatorToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<And>(grammar, "&&")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override And BuildImpl(Range range) => new(range);
    }
}