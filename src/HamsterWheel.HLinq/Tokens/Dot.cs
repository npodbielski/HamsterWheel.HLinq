namespace HamsterWheel.HLinq.Tokens;

public sealed class Dot : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Dot>(grammar, ".")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static Dot Build(Range range) => new() { Range = range };
    public static Dot Empty { get; } = new() { Range = default };
}