using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<NameOrValue>(grammar)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, char? nextNext,
            List<IToken> previousToken)
        {
            // a value may contain single dots (i.e. `1.21` dates or floats) but never a `..`,
            // end the value here so the `..` range operator of take[start..end] can be tokenized
            if (next == '.' && nextNext == '.' && subset[^1] != '.' && PreviousTokensMatch(previousToken))
            {
                return 100;
            }

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