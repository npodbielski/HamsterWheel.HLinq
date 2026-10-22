namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class PropertyName : MemberAccess
{
    public sealed class Word() : TokenWord<PropertyName>();

    public static PropertyName Build(Range range) => new() { Range = range };
    public static PropertyName Empty { get; } = new() { Range = default };
}