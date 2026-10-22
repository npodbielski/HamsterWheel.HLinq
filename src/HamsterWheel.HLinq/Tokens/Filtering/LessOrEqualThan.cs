namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LessOrEqualThan : TokenBase, IComparisonToken
{
    public sealed class Word() : TokenWord<LessOrEqualThan>("<=");

    public static LessOrEqualThan Build(Range range) => new() { Range = range };
    public static LessOrEqualThan Empty { get; } = new() { Range = default };
}