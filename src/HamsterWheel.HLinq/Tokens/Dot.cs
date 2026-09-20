using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class Dot : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Dot>(grammar, ".")
    {
    }

    public static Dot Build(Range range) => new() { Range = range };
    public static Dot Empty { get; } = new() { Range = default };
}