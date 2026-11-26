
namespace HamsterWheel.HLinq.Tokens.Selecting;

/// <summary>
/// The `-` between two numeric answers of one member.
/// </summary>
public sealed class Minus : TokenBase, IMathOperatorToken
{
    // ReSharper disable once UnusedType.Global -> used by the Grammar
    public sealed class Word() : TokenWord<Minus>("-");

    public static Minus Build(Range range) => new() { Range = range };
    public static Minus Empty { get; } = new() { Range = default };
}
