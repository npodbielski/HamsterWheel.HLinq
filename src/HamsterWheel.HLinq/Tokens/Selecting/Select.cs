namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Select : TokenBase, IClauseRootToken
{
    // ReSharper disable once UnusedType.Global -> used by the grammar
    public sealed class Word() : TokenWord<Select>("select");

    public static Select Build(Range range) => new() { Range = range };
    public static Select Empty { get; } = new() { Range = default };
}