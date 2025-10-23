namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Assignment>(grammar, "=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<Assignment>(previousToken);

        protected override Assignment BuildImpl(Range range) => new(range);
    }
}