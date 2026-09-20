using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Count : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Count>(grammar, "count")
    {
    }

    public static Count Build(Range range) => new() { Range = range };
    public static Count Empty { get; } = new() { Range = default };
}