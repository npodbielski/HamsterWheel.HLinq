namespace HamsterWheel.HLinq.Tokens.Ordering;

public sealed class ThenByDescending : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<ThenByDescending>("thenByDescending");

    public static ThenByDescending Build(Range range) => new() { Range = range };
    public static ThenByDescending Empty { get; } = new() { Range = default };
}