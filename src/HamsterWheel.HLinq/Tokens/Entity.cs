namespace HamsterWheel.HLinq.Tokens;

public sealed class Entity(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Entity>(grammar, "x")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<Entity>(previousToken);

        protected override Entity BuildImpl(Range range) => new(range);
    }
}