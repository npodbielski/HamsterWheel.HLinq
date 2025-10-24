namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Comma : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Comma>(grammar, ",")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static Comma Build(Range range) => new() { Range = range };
    public static Comma Empty { get; } = new() { Range = default };
}