namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Take : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<Take>("take");

    public static Take Build(Range range) => new() { Range = range };
    public static Take Empty { get; } = new() { Range = default };
}