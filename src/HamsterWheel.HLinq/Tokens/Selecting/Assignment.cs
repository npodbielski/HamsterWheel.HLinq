using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Assignment>(grammar, "=")
    {
        //`=` renames in select, `==` compares in where; until the grammar tells the two roots apart,
        //a following `=` must not complete an Assignment on the first character of an Equality
        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            Rule.NextCharMatch(next);
    }

    public static Assignment Build(Range range) => new() { Range = range };
    public static Assignment Empty { get; } = new() { Range = default };
}