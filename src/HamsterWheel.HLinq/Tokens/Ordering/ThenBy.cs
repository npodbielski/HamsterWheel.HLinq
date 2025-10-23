namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenBy(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenBy>(grammar, "thenBy")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<ThenBy>(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Grammar.NextCharIsAllowed<ThenBy>(next);

        protected override ThenBy BuildImpl(Range range) => new(range);
    }
}