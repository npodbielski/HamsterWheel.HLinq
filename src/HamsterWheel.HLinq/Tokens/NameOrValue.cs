using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<NameOrValue>(delimiters:
    [
        Equality.TokenValue.AsSpan()[0],
        Inequality.TokenValue.AsSpan()[0],
        LessThan.TokenValue.AsSpan()[0],
        LessOrEqualThan.TokenValue.AsSpan()[0],
        GreaterThan.TokenValue.AsSpan()[0],
        GreaterOrEqualThan.TokenValue.AsSpan()[0],
        RightSquareBracket.TokenValue.AsSpan()[0],
        RightCircleBracket.TokenValue.AsSpan()[0],
        And.TokenValue.AsSpan()[0],
        Or.TokenValue.AsSpan()[0],
        Comma.TokenValue.AsSpan()[0]
    ])
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            previousTokens is
            [
                .., _, IComparisonToken or LeftCircleBracket or LeftSquareBracket or Comma
                or Assignment
            ];

        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
        {
            if (!PreviousTokensMatch(previousToken)) return 0;

            var possibility = 50;
            if (subset.Length == 1 && subset is "x" && next is '.')
            {
                possibility = 0;
                return possibility;
            }

            if (next is not null && Delimiters?.Contains(next.Value) == true) possibility += 50;

            return possibility;
        }

        protected override NameOrValue BuildImpl(Range range) => new(range);
    }
}