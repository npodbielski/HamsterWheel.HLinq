namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class MethodName : MemberAccess
{
    public sealed class Word() : TokenWord<MethodName>();

    public static MethodName Build(Range range) => new() { Range = range };
    public static MethodName Empty { get; } = new() { Range = default };
}