using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterThan : TokenBase, IComparisonToken
{
    public sealed class Word() : TokenWord<GreaterThan>(">")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) =>
            grammar.GetRuleFor(ForType).NextCharMatch(next);
    }

    public static GreaterThan Build(Range range) => new() { Range = range };
    public static GreaterThan Empty { get; } = new() { Range = default };
}