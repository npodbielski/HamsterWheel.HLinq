using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LeftCircleBracket : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LeftCircleBracket>(grammar, "(")
    {
    }

    public static LeftCircleBracket Build(Range range) => new() { Range = range };
    public static LeftCircleBracket Empty { get; } = new() { Range = default };
}