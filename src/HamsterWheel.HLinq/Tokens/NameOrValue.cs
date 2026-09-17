using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<NameOrValue>(grammar)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
        {
            // a name or value may contain single dots (i.e. `1.21` floats or dates) but never a `..`,
            // that is the range operator (which reads the number in front of it as its own grammar)
            for (var dotIndex = 0; dotIndex + 1 < subset.Length; dotIndex++)
            {
                if (subset[dotIndex] == '.' && subset[dotIndex + 1] == '.')
                {
                    return 0;
                }
            }

            if (!PreviousTokensMatch(previousToken))
            {
                return 0;
            }

            var possibility = 50;
            if (next is not null && Delimiters?.Contains(next.Value) == true)
            {
                possibility += 50;
            }

            return possibility;
        }
    }

    public static NameOrValue Build(Range range) => new() { Range = range };
    public static NameOrValue Empty { get; } = new() { Range = default };
}