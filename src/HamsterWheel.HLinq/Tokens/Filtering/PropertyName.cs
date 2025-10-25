namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class PropertyName : MemberAccess
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<PropertyName>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static PropertyName Build(Range range) => new() { Range = range };
    public static PropertyName Empty { get; } = new() { Range = default };
}