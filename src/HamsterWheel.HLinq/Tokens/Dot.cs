namespace HamsterWheel.HLinq.Tokens;

public sealed class Dot(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Dot>(grammar, TokenValue)
    {
        public const string TokenValue = ".";

        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<Dot>(previousToken);

        protected override Dot BuildImpl(Range range) => new(range);
    }
}