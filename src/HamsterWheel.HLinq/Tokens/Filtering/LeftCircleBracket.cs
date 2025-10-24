namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LeftCircleBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LeftCircleBracket>(grammar, "(")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override LeftCircleBracket BuildImpl(Range range) => new(range);
    }
}