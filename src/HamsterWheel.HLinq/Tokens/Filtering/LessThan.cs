namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility() : TokenPossibility<LessThan>(TokenValue)
    {
        public const string TokenValue = "<";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            GrammarRules.SelectPreviousTokenMatch<LessThan>(previousToken);

        protected override LessThan BuildImpl(Range range) => new(range);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next switch
            {
                '=' => false,
                _ => true
            };
    }
}