using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Comma : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Comma>(grammar, ",")
    {
    }

    public static Comma Build(Range range) => new() { Range = range };
    public static Comma Empty { get; } = new() { Range = default };
}