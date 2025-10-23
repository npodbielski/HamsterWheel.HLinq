namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar)
        : TokenPossibility<NameOrValue>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Grammar.PreviousTokensMatch<NameOrValue>(previousTokens);

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

        protected override NameOrValue BuildImpl(Range range) => new(range);
    }
}