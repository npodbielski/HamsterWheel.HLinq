
namespace HamsterWheel.HLinq.Tokens.Selecting;

/// <summary>
/// The `*` between two numeric answers of one member.
/// </summary>
public sealed class Multiply : TokenBase, IMathOperatorToken
{
    // ReSharper disable once UnusedType.Global -> used by the Grammar
    public sealed class Word() : TokenWord<Multiply>("*");

    public static Multiply Build(Range range) => new() { Range = range };
    public static Multiply Empty { get; } = new() { Range = default };
}
