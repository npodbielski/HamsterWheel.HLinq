namespace HamsterWheel.HLinq.Tokens;

public sealed class LeftSquareBracket : TokenBase
{
    public const char Open = '[';

    // ReSharper disable once UnusedType.Global -> it is used by the Grammar
    public sealed class Word() : TokenWord<LeftSquareBracket>(Open.ToString());

    public static LeftSquareBracket Build(Range range) => new() { Range = range };
    public static LeftSquareBracket Empty { get; } = new() { Range = default };
}