using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Grouping;

/// <summary>
/// Represents the <c>groupBy</c> keyword token in an HLinq query string.
/// </summary>
public sealed class GroupBy : TokenBase
{
    /// <summary>
    /// Token possibility that matches the <c>groupBy</c> keyword.
    /// </summary>
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<GroupBy>(grammar, "groupBy")
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    /// <summary>Builds a <see cref="GroupBy"/> token covering the supplied <paramref name="range"/>.</summary>
    public static GroupBy Build(Range range) => new() { Range = range };

    /// <summary>A default empty instance used for parser example token sequences.</summary>
    public static GroupBy Empty { get; } = new() { Range = default };
}
