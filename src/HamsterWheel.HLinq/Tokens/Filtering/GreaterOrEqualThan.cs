namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class GreaterOrEqualThan : TokenBase, IComparisonToken
{
    public sealed class Word() : TokenWord<GreaterOrEqualThan>(">=");

    public static GreaterOrEqualThan Build(Range range) => new() { Range = range };
    public static GreaterOrEqualThan Empty { get; } = new() { Range = default };
}