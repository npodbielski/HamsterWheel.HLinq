using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterThan : TokenBase, IComparisonToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<GreaterThan>(grammar, ">")
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Rule.PreviousTokenMatch(previousToken);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    public static GreaterThan Build(Range range) => new() { Range = range };
    public static GreaterThan Empty { get; } = new() { Range = default };
}