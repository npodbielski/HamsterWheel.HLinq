namespace HamsterWheel.HLinq.Tokens;

public sealed class RightSquareBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RightSquareBracket>(grammar, "]")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override RightSquareBracket BuildImpl(Range range) => new(range);
    }
}