namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Comma : TokenBase
{
    // ReSharper disable once UnusedType.Global - used in tokenizer implicitly
    public sealed class Word() : TokenWord<Comma>(",");

    public static Comma Build(Range range) => new() { Range = range };
    public static Comma Empty { get; } = new() { Range = default };
}