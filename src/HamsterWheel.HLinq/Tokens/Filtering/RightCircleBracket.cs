using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class RightCircleBracket: TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RightCircleBracket>(grammar, ")")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static RightCircleBracket Build(Range range) => new() { Range = range };
    public static RightCircleBracket Empty { get; } = new() { Range = default };
}