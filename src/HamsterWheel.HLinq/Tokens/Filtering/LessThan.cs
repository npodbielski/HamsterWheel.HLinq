using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessThan : TokenBase, IComparisonToken
{
    public sealed class Word() : TokenWord<LessThan>("<")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) =>
            grammar.GetRuleFor(ForType).NextCharMatch(next);
    }

    public static LessThan Build(Range range) => new() { Range = range };
    public static LessThan Empty { get; } = new() { Range = default };
}