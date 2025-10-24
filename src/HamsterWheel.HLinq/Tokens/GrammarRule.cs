namespace HamsterWheel.HLinq.Tokens;

public class GrammarRule<T>(
    bool canBeFirst,
    Type[]? canBeAfter = null,
    Func<IReadOnlyList<IToken>, bool>[]? previousTokensMatchers = null,
    Func<char?, bool>? nextCharMatcher = null) : IGrammarRule
{
    public Type ForType { get; } = typeof(T);
    public bool IsFor<TToken>() => ForType == typeof(TToken);
    public bool CanBeFirst { get; } = canBeFirst;
    public Func<IReadOnlyList<IToken>, bool>[] PreviousTokensMatchers { get; } = previousTokensMatchers ?? [];
    public Type[] CanBeAfter { get; } = canBeAfter ?? [];

    public bool PreviousTokensMatch(IReadOnlyList<IToken> previousTokens) =>
        previousTokens.Count == 0 && CanBeFirst
        || PreviousTokensMatchers.Length > 0 && PreviousTokensMatchers.Any(m => m(previousTokens));

    public bool NextCharMatch(char? next) => nextCharMatcher is null || nextCharMatcher(next);

    public bool PreviousTokenMatch(IToken previousToken) => CanBeAfter.Contains(previousToken.GetType());
}