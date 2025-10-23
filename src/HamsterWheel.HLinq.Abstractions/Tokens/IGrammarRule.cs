namespace HamsterWheel.HLinq.Tokens;

public interface IGrammarRule
{
    bool IsFor<TToken>();
    bool CanBeFirst { get; }
    Func<List<IToken>, bool>[] PreviousTokensMatchers { get; }
    Type[] CanBeAfter { get; }
    Type ForType { get; }
}