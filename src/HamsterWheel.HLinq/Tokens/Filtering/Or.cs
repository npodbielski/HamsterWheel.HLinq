namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Or : TokenBase, ILogicalOperatorToken
{
    public sealed class Word() : TokenWord<Or>("||");

    public static Or Build(Range range) => new() { Range = range };
    public static Or Empty { get; } = new() { Range = default };
}