using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<NameOrValue>(grammar)
    {

        protected override TokenPattern BuildPattern() => new NameOrValuePattern(() => Delimiters);
    }

    public static NameOrValue Build(Range range) => new() { Range = range };
    public static NameOrValue Empty { get; } = new() { Range = default };
}