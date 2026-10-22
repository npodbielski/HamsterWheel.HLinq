namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Equality : TokenBase, IComparisonToken
{
    // ReSharper disable once UnusedType.Global - used in tokenizer implicitly
    public sealed class Word() : TokenWord<Equality>("==");

    public static Equality Build(Range range) => new() { Range = range };
    public static Equality Empty { get; } = new() { Range = default };
}