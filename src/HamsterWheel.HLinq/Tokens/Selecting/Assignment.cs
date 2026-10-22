namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment : TokenBase
{
    /// <summary>Token character</summary>
    public const char Sign = '=';

    // ReSharper disable once UnusedType.Global - used in tokenizer implicitly
    public sealed class Word() : TokenWord<Assignment>(Sign.ToString());

    public static Assignment Build(Range range) => new() { Range = range };
    public static Assignment Empty { get; } = new() { Range = default };
}