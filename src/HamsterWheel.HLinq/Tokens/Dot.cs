namespace HamsterWheel.HLinq.Tokens;

public sealed class Dot : TokenBase
{
    // ReSharper disable once UnusedType.Global - used in tokenizer implicitly
    public sealed class Word() : TokenWord<Dot>(".");

    public static Dot Build(Range range) => new() { Range = range };
    public static Dot Empty { get; } = new() { Range = default };
}