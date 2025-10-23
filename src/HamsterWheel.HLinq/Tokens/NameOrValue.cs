using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<NameOrValue>(delimiters:
    [
        Equality.Possibility.TokenValue.AsSpan()[0],
        Inequality.Possibility.TokenValue.AsSpan()[0],
        LessThan.Possibility.TokenValue.AsSpan()[0],
        LessOrEqualThan.Possibility.TokenValue.AsSpan()[0],
        GreaterThan.Possibility.TokenValue.AsSpan()[0],
        GreaterOrEqualThan.Possibility.TokenValue.AsSpan()[0],
        RightSquareBracket.Possibility.TokenValue.AsSpan()[0],
        RightCircleBracket.Possibility.TokenValue.AsSpan()[0],
        And.Possibility.TokenValue.AsSpan()[0],
        Or.Possibility.TokenValue.AsSpan()[0],
        Comma.Possibility.TokenValue.AsSpan()[0]
    ])
    {
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.PreviousTokensMatch<NameOrValue>(previousTokens);

        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
        {
            if (!PreviousTokensMatch(previousToken))
            {
                return 0;
            }

            var possibility = 50;
            if (subset.Length == 1 && subset is "x" && next is '.')
            {
                possibility = 0;
                return possibility;
            }

            if (next is not null && Delimiters?.Contains(next.Value) == true)
            {
                possibility += 50;
            }

            return possibility;
        }

        protected override NameOrValue BuildImpl(Range range) => new(range);
    }
}