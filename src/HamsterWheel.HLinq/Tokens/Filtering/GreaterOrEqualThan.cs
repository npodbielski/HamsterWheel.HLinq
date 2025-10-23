namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility() : TokenPossibility<GreaterOrEqualThan>(TokenValue)
    {
        public const string TokenValue = ">=";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.PreviousTokenMatch<GreaterOrEqualThan>(previousToken);

        protected override GreaterOrEqualThan BuildImpl(Range range) => new(range);
    }
}