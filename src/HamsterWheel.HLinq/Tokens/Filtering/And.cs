using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class And : TokenBase, ILogicalOperatorToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<And>(grammar, "&&")
    {
    }

    public static And Build(Range range) => new() { Range = range };
    public static And Empty { get; } = new() { Range = default };
}