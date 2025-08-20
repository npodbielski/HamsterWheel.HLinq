namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class GreaterOrEqualThan(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = ">=";
    public IToken Token => this;

    public sealed class Possibility() : TokenPossibility<GreaterOrEqualThan>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken)
        {
            return previousToken is PropertyAccess or RightSquareBracket;
        }

        protected override GreaterOrEqualThan BuildImpl(Range range)
        {
            return new GreaterOrEqualThan(range);
        }
    }
}