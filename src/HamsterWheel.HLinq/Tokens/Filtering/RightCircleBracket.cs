namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class RightCircleBracket : TokenBase
{
    public sealed class Word() : TokenWord<RightCircleBracket>(")");

    public static RightCircleBracket Build(Range range) => new() { Range = range };
    public static RightCircleBracket Empty { get; } = new() { Range = default };
}