using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class MethodName : MemberAccess
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<MethodName>(grammar)
    {
    }

    public static MethodName Build(Range range) => new() { Range = range };
    public static MethodName Empty { get; } = new() { Range = default };
}