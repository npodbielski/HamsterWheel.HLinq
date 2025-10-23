namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class PropertyAccess(Range range) : MemberAccess(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<PropertyAccess>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<PropertyAccess>(previousTokens);

        protected override PropertyAccess BuildImpl(Range range) => new(range);
    }
}