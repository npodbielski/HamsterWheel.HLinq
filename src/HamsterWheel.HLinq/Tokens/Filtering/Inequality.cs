using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Inequality : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Inequality>(grammar, "!=")
    {
    }

    public static Inequality Build(Range range) => new() { Range = range };
    public static Inequality Empty { get; } = new() { Range = default };
}