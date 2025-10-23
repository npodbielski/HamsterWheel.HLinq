namespace HamsterWheel.HLinq.Tokens;

public sealed class Entity(Range range) : TokenBase(range)
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<Entity>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<Entity>(previousToken);

        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
        {
            if (!PreviousTokensMatch(previousToken)) return 0;

            var possibility = 0;
            if (subset.Length != 1)
            {
                return 100 / subset.Length;
            }

            possibility += 50;
            if (subset.Length == 1 && char.IsLetter(subset[0]) && next is '.')
            {
                possibility += 50;
            }

            return possibility;
        }

        protected override Entity BuildImpl(Range range) => new(range);
    }
}