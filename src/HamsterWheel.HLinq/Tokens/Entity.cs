using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tokens.Select;

namespace HamsterWheel.HLinq.Tokens;

public sealed class Entity(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Entity>(delimiters: [Dot.TokenValue.AsSpan()[0]])
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken)
        {
            return previousToken is LeftSquareBracket or And or Or or Comma or LeftCircleBracket or Assignment;
        }

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

        protected override Entity BuildImpl(Range range)
        {
            return new Entity(range);
        }
    }
}