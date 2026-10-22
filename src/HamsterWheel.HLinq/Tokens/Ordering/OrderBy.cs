using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class OrderBy : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<OrderBy>("orderBy")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) =>
            grammar.GetRuleFor(ForType).NextCharMatch(next);
    }

    public static OrderBy Build(Range range) => new() { Range = range };
    public static OrderBy Empty { get; } = new() { Range = default };
}