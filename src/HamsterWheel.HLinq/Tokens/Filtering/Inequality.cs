namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Inequality : TokenBase, IComparisonToken
{
    public sealed class Word() : TokenWord<Inequality>("!=");

    public static Inequality Build(Range range) => new() { Range = range };
    public static Inequality Empty { get; } = new() { Range = default };
}