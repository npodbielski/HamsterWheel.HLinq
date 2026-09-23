using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderByDescending : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar)
        : TokenPossibility<OrderByDescending>(grammar, "orderByDescending")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) => Rule.NextCharMatch(next);
    }

    public static OrderByDescending Build(Range range) => new() { Range = range };
    public static OrderByDescending Empty { get; } = new() { Range = default };
}