namespace HamsterWheel.HLinq.Tokens;

public sealed class RightSquareBracket : TokenBase
{
    public sealed class Word() : TokenWord<RightSquareBracket>("]");

    public static RightSquareBracket Build(Range range) => new() { Range = range };
    public static RightSquareBracket Empty { get; } = new() { Range = default };
}