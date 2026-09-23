using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenBy : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<ThenBy>(grammar, "thenBy")
    {

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    public static ThenBy Build(Range range) => new() { Range = range };
    public static ThenBy Empty { get; } = new() { Range = default };
}