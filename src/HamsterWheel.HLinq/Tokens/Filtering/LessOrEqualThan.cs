using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessOrEqualThan : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<LessOrEqualThan>(grammar, "<=")
    {
    }

    public static LessOrEqualThan Build(Range range) => new() { Range = range };
    public static LessOrEqualThan Empty { get; } = new() { Range = default };
}