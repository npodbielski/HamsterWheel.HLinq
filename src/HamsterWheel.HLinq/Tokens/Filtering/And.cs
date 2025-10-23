namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And(Range range) : TokenBase(range), ILogicalOperatorToken
{
    public sealed class Possibility() : TokenPossibility<And>(TokenValue)
    {
        public const string TokenValue = "&&";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.PreviousTokenMatch<And>(previousToken);

        protected override And BuildImpl(Range range) => new(range);
    }
}