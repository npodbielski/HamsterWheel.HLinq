using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment : TokenBase
{
    /// <summary>Token character</summary>
    public const char Sign = '=';

    // ReSharper disable once UnusedType.Global
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Assignment>(grammar, Sign.ToString());

    public static Assignment Build(Range range) => new() { Range = range };
    public static Assignment Empty { get; } = new() { Range = default };
}