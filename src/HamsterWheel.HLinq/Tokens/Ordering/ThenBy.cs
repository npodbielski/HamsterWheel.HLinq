using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenBy : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<ThenBy>("thenBy")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) =>
            grammar.GetRuleFor(ForType).NextCharMatch(next);
    }

    public static ThenBy Build(Range range) => new() { Range = range };
    public static ThenBy Empty { get; } = new() { Range = default };
}