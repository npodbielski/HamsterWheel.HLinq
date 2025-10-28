using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Where>(grammar, "where")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);
    }

    public static Where Build(Range range) => new() { Range = range };
    public static Where Empty { get; } = new() { Range = default };
}