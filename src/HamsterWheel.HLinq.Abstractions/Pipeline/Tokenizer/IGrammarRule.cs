using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// The adjacency rule of a token: after which sequences of tokens it may appear, declared
/// exclusively through <see cref="PreviousTokensMatchers" />. The delimiter set of free
/// text tokens is derived from these matchers, so a rule can never silently fail to make
/// the token that follows it a delimiter.
/// </summary>
public interface IGrammarRule
{
    bool IsFor<TToken>();
    bool CanBeFirst { get; }
    Func<IReadOnlyList<IToken>, bool>[] PreviousTokensMatchers { get; }
    Type ForType { get; }
    bool PreviousTokensMatch(IReadOnlyList<IToken> previousTokens);
    bool NextCharMatch(char? next);

    /// <summary>
    /// Whether this token may stand in the clause opened by <paramref name="currentClauseRoot" />
    /// (null: nothing of the kind has been committed yet). Which clause a token belongs to is not
    /// adjacency - it is not about the token before it - so it is declared here rather than as a
    /// matcher, and the tokenizer knows the current clause without being asked to look for it.
    /// </summary>
    bool MatchesClauseRoot(Type? currentClauseRoot);
}
