namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue : TokenBase
{
    public sealed class Possibility(IGrammar grammar)
        : TokenPossibility<NameOrValue>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
        {
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