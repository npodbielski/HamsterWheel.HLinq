using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderByDescending : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<OrderByDescending>("orderByDescending")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) =>
            grammar.GetRuleFor(ForType).NextCharMatch(next);
    }

    public static OrderByDescending Build(Range range) => new() { Range = range };
    public static OrderByDescending Empty { get; } = new() { Range = default };
}