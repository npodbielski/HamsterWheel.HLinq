namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And : TokenBase, ILogicalOperatorToken
{
    // ReSharper disable once UnusedType.Global - used in tokenizer implicitly
    public sealed class Word() : TokenWord<And>("&&");

    public static And Build(Range range) => new() { Range = range };
    public static And Empty { get; } = new() { Range = default };
}