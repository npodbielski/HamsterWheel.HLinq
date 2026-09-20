using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Equality : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Equality>(grammar, "==")
    {
    }

    public static Equality Build(Range range) => new() { Range = range };
    public static Equality Empty { get; } = new() { Range = default };
}