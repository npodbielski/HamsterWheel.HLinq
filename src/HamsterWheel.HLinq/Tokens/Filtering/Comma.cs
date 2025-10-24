namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Comma(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Comma>(grammar, ",")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override Comma BuildImpl(Range range) => new(range);
    }
}