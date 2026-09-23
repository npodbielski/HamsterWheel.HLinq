using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket : TokenBase
{
    // ReSharper disable once UnusedType.Global -> it is used by the Grammar
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LeftSquareBracket>(grammar, "[")
    {
    }

    public static LeftSquareBracket Build(Range range) => new() { Range = range };
    public static LeftSquareBracket Empty { get; } = new() { Range = default };
}