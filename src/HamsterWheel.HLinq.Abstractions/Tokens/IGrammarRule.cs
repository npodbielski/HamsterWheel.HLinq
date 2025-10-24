namespace HamsterWheel.HLinq.Tokens;

public interface IGrammarRule
{
    bool IsFor<TToken>();
    bool CanBeFirst { get; }
    Func<IReadOnlyList<IToken>, bool>[] PreviousTokensMatchers { get; }
    Type[] CanBeAfter { get; }
    Type ForType { get; }
    bool PreviousTokensMatch(IReadOnlyList<IToken> previousTokens);
    bool PreviousTokenMatch(IToken previousToken);
    bool NextCharMatch(char? next);
}