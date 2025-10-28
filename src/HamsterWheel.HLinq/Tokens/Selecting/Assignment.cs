using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Assignment>(grammar, "=")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);
    }

    public static Assignment Build(Range range) => new() { Range = range };
    public static Assignment Empty { get; } = new() { Range = default };
}