using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Paging;

/// <summary>
/// The <c>..</c> operator of the <c>take[start..end]</c> range paging syntax.
/// </summary>
public sealed class RangeOperator : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RangeOperator>(grammar, "..")
    {
        // matcher based rule (not AllowedPreviousTokens) on purpose, so '.' does not become
        // a delimiter ending NameOrValue tokens which may legally contain single dots (i.e. `1.21`)
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static RangeOperator Build(Range range) => new() { Range = range };
    public static RangeOperator Empty { get; } = new() { Range = default };
}
