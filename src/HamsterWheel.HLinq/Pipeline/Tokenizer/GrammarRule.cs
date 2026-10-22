using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class GrammarRule<T>(
    bool canBeFirst,
    Func<IReadOnlyList<IToken>, bool>[] previousTokensMatchers,
    char[]? nextCharsAllowed = null,
    char[]? nextCharsRefused = null,
    Type? insideClauseOf = null,
    Type? outsideClauseOf = null) : IGrammarRule
{
    public Type ForType { get; } = typeof(T);
    public bool IsFor<TToken>() => ForType == typeof(TToken);
    public bool CanBeFirst { get; } = canBeFirst;
    public Func<IReadOnlyList<IToken>, bool>[] PreviousTokensMatchers { get; } = previousTokensMatchers;

    public bool PreviousTokensMatch(IReadOnlyList<IToken> previousTokens)
    {
        if (previousTokens.Count == 0)
        {
            return CanBeFirst;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery - use loop to avoid allocation with lambda
        foreach (var matcher in PreviousTokensMatchers)
        {
            if (matcher(previousTokens))
            {
                return true;
            }
        }

        return false;
    }

    public bool NextCharMatch(char? next) =>
        (nextCharsAllowed is null || next is not null && nextCharsAllowed.Contains(next.Value))
        && (nextCharsRefused is null || next is null || !nextCharsRefused.Contains(next.Value));

    public bool MatchesClauseRoot(Type? currentClauseRoot) =>
        (insideClauseOf is null || insideClauseOf == currentClauseRoot)
        && (outsideClauseOf is null || outsideClauseOf != currentClauseRoot);
}
