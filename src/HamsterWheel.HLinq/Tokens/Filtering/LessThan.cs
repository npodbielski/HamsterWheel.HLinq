namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessThan(Range range) : TokenBase(range), IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LessThan>(grammar, "<")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<LessThan>(previousToken);

        protected override LessThan BuildImpl(Range range) => new(range);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next switch
            {
                '=' => false,
                _ => true
            };
    }
}