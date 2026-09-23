using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens;

public sealed class Entity : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Entity>(grammar, "x")
    {
        //`x` is a single letter that can start a value (`where[x.Name==xander]`), so it has to end
        //where the grammar says a token may follow it instead of after its own character
        protected override TokenPattern BuildPattern() => new WordKeywordPattern(Keyword!, () => Delimiters);
    }

    public static Entity Build(Range range) => new() { Range = range };
    public static Entity Empty { get; } = new() { Range = default };
}