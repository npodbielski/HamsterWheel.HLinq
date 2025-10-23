namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenByDescending(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenByDescending>(grammar, "thenByDescending")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<ThenByDescending>(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Grammar.NextCharIsAllowed<ThenByDescending>(next);

        protected override ThenByDescending BuildImpl(Range range) => new(range);
    }
}