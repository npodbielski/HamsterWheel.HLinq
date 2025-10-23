namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Inequality(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Inequality>(grammar, TokenValue)
    {
        public const string TokenValue = "!=";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<Inequality>(previousToken);

        protected override Inequality BuildImpl(Range range) => new(range);
    }
}