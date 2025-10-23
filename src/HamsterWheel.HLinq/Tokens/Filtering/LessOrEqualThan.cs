namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility() : TokenPossibility<LessOrEqualThan>(TokenValue)
    {
        public const string TokenValue = "<=";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.PreviousTokenMatch<LessOrEqualThan>(previousToken);

        protected override LessOrEqualThan BuildImpl(Range range) => new(range);
    }
}