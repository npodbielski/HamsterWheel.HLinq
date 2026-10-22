namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Count : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<Count>("count");

    public static Count Build(Range range) => new() { Range = range };
    public static Count Empty { get; } = new() { Range = default };
}