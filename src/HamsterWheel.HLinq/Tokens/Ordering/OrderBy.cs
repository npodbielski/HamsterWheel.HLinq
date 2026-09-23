using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderBy : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<OrderBy>(grammar, "orderBy")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    public static OrderBy Build(Range range) => new() { Range = range };
    public static OrderBy Empty { get; } = new() { Range = default };
}