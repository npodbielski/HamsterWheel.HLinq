using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class GrammarRule<T>(
    bool canBeFirst,
    Func<IReadOnlyList<IToken>, bool>[] previousTokensMatchers,
    Func<char?, bool>? nextCharMatcher = null) : IGrammarRule
{
    public Type ForType { get; } = typeof(T);
    public bool IsFor<TToken>() => ForType == typeof(TToken);
    public bool CanBeFirst { get; } = canBeFirst;
    public Func<IReadOnlyList<IToken>, bool>[] PreviousTokensMatchers { get; } = previousTokensMatchers;

    public bool PreviousTokensMatch(IReadOnlyList<IToken> previousTokens) =>
        previousTokens.Count == 0 && CanBeFirst
        || PreviousTokensMatchers.Any(m => m(previousTokens));

    public bool NextCharMatch(char? next) => nextCharMatcher is null || nextCharMatcher(next);
}
