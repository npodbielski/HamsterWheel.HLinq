using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LeftSquareBracket>(grammar, "[")
    {
    }

    public static LeftSquareBracket Build(Range range) => new() { Range = range };
    public static LeftSquareBracket Empty { get; } = new() { Range = default };
}