namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class MethodName : MemberAccess
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<MethodName>(grammar)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static MethodName Build(Range range) => new() { Range = range };
    public static MethodName Empty { get; } = new() { Range = default };
}