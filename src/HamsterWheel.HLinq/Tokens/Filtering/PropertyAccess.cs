namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class PropertyAccess : MemberAccess
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<PropertyAccess>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static PropertyAccess Build(Range range) => new() { Range = range };
    public static PropertyAccess Empty { get; } = new() { Range = default };
}