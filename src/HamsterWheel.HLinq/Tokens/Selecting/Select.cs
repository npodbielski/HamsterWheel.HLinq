using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Select : TokenBase, IClauseRootToken
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Select>(grammar, "select")
    {
    }

    public static Select Build(Range range) => new() { Range = range };
    public static Select Empty { get; } = new() { Range = default };
}