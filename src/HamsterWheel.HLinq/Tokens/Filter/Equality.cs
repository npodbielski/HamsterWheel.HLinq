namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class Equality(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = "==";
    public IToken Token => this;

    public sealed class Possibility() : TokenPossibility<Equality>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken)
        {
            return previousToken is PropertyAccess or RightSquareBracket;
        }

        protected override Equality BuildImpl(Range range)
        {
            return new Equality(range);
        }
    }
}