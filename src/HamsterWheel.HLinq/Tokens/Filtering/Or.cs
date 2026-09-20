using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Or : TokenBase, ILogicalOperatorToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Or>(grammar, "||")
    {
    }

    public static Or Build(Range range) => new() { Range = range };
    public static Or Empty { get; } = new() { Range = default };
}