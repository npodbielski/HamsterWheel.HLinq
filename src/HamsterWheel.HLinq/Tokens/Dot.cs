namespace HamsterWheel.HLinq.Tokens;

public sealed class Dot(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Dot>(grammar, ".")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override Dot BuildImpl(Range range) => new(range);
    }
}