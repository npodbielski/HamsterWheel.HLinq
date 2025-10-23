namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And(Range range) : TokenBase(range), ILogicalOperatorToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<And>(grammar, TokenValue)
    {
        public const string TokenValue = "&&";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<And>(previousToken);

        protected override And BuildImpl(Range range) => new(range);
    }
}