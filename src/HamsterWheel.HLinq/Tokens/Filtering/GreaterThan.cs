namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<GreaterThan>(grammar, TokenValue)
    {
        public const string TokenValue = ">";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<GreaterThan>(previousToken);

        protected override GreaterThan BuildImpl(Range range) => new(range);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Grammar.NextCharIsAllowed<GreaterThan>(next);
    }
}