namespace HamsterWheel.HLinq.Tokens.Grouping;

public sealed class GroupBy : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<GroupBy>("groupBy");

    public static GroupBy Build(Range range) => new() { Range = range };
    public static GroupBy Empty { get; } = new() { Range = default };
}
