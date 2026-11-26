
namespace HamsterWheel.HLinq.Tokens.Selecting;

/// <summary>
/// The `/` between two numeric answers of one member.
/// </summary>
public sealed class Divide : TokenBase, IMathOperatorToken
{
    // ReSharper disable once UnusedType.Global -> used by the Grammar
    public sealed class Word() : TokenWord<Divide>("/");

    public static Divide Build(Range range) => new() { Range = range };
    public static Divide Empty { get; } = new() { Range = default };
}
