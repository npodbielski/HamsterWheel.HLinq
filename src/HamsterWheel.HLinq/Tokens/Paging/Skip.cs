namespace HamsterWheel.HLinq.Tokens.Paging;

public sealed class Skip : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<Skip>("skip");

    public static Skip Build(Range range) => new() { Range = range };
    public static Skip Empty { get; } = new() { Range = default };
}