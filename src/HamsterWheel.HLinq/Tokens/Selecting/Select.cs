namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Select(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Select>(grammar, "select")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override Select BuildImpl(Range range) => new(range);
    }
}