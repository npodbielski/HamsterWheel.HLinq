using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Take>(grammar, "take")
    {
    }

    public static Take Build(Range range) => new() { Range = range };
    public static Take Empty { get; } = new() { Range = default };
}