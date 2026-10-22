namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Where : TokenBase, IClauseRootToken
{
    public sealed class Word() : TokenWord<Where>("where");

    public static Where Build(Range range) => new() { Range = range };
    public static Where Empty { get; } = new() { Range = default };
}