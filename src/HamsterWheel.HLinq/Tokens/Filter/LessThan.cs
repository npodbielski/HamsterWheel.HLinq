namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class LessThan(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = "<";

    public sealed class Possibility() : TokenPossibility<LessThan>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is PropertyAccess or RightSquareBracket;

        protected override LessThan BuildImpl(Range range) => new(range);

        protected override bool NextIsAllowedWhenKeywordMatch(char? next) =>
            next switch
            {
                '=' => false,
                _ => true
            };
    }
}