
namespace HamsterWheel.HLinq.Tokens.Selecting;

/// <summary>
/// The `+` of one member: text joins through it, numbers sum.
/// </summary>
public sealed class Plus : TokenBase, IMathOperatorToken
{
    // ReSharper disable once UnusedType.Global -> used by the Grammar
    public sealed class Word() : TokenWord<Plus>("+");

    public static Plus Build(Range range) => new() { Range = range };
    public static Plus Empty { get; } = new() { Range = default };
}
