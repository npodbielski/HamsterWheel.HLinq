using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenByDescending : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenByDescending>(grammar, "thenByDescending")
    {
    }

    public static ThenByDescending Build(Range range) => new() { Range = range };
    public static ThenByDescending Empty { get; } = new() { Range = default };
}