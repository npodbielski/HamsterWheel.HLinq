namespace HamsterWheel.HLinq.Tokens;

public class GrammarRule<T>(bool canBeFirst, Type[]? canBeAfter = null,
    Func<List<IToken>, bool>[]? previousTokensMatchers = null) : IGrammarRule
{
    public Type ForType => typeof(T);
    public bool IsFor<TToken>() => typeof(TToken) == typeof(T);
    public bool CanBeFirst => canBeFirst;
    public Func<List<IToken>, bool>[] PreviousTokensMatchers { get; } = previousTokensMatchers ?? [];
    public Type[] CanBeAfter { get; } = canBeAfter ?? [];
}