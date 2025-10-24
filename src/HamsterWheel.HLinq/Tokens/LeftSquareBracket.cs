namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LeftSquareBracket>(grammar, "[")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
           Rule.PreviousTokenMatch(previousToken);

        protected override LeftSquareBracket BuildImpl(Range range) => new(range);
    }
}