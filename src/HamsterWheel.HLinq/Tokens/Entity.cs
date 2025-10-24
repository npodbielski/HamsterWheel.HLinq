namespace HamsterWheel.HLinq.Tokens;

public sealed class Entity : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Entity>(grammar, "x")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static Entity Build(Range range) => new() { Range = range };
    public static Entity Empty { get; } = new() { Range = default };
}