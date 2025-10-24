namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Where>(grammar, "where")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override Where BuildImpl(Range range) => new(range);
    }
}