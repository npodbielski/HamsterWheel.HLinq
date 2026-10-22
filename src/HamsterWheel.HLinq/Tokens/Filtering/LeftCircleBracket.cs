namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class LeftCircleBracket : TokenBase
{
    public sealed class Word() : TokenWord<LeftCircleBracket>("(");

    public static LeftCircleBracket Build(Range range) => new() { Range = range };
    public static LeftCircleBracket Empty { get; } = new() { Range = default };
}