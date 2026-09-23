using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class GrammarRule<T>(
    bool canBeFirst,
    Func<IReadOnlyList<IToken>, bool>[] previousTokensMatchers,
    Func<char?, bool>? nextCharMatcher = null,
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

        //use loop to avoid allocation with lambda
        foreach (var matcher in PreviousTokensMatchers)
        {
            if (matcher(previousTokens))
            {
                return true;
            }
        }

        return false;
    }

    public bool NextCharMatch(char? next) => nextCharMatcher is null || nextCharMatcher(next);

    public bool MatchesClauseRoot(Type? currentClauseRoot) =>
        (insideClauseOf is null || insideClauseOf == currentClauseRoot)
        && (outsideClauseOf is null || outsideClauseOf != currentClauseRoot);
}
