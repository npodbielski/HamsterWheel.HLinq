namespace HamsterWheel.HLinq.Tokens;

public sealed class RightSquareBracket : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RightSquareBracket>(grammar, "]")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static RightSquareBracket Build(Range range) => new() { Range = range };
    public static RightSquareBracket Empty { get; } = new() { Range = default };
}