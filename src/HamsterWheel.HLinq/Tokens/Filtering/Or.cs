namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Or(Range range) : TokenBase(range), ILogicalOperatorToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Or>(grammar, "||")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override Or BuildImpl(Range range) => new(range);
    }
}