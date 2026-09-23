using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Skip>(grammar, "skip")
    {
    }

    public static Skip Build(Range range) => new() { Range = range };
    public static Skip Empty { get; } = new() { Range = default };
}