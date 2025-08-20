namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class And(Range range) : TokenBase(range), IConditionalLogicalOperationToken
{
    public const string TokenValue = "&&";
    public TokenBase Token => (TokenBase)this;

    public sealed class Possibility() : TokenPossibility<And>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken)
        {
            return previousToken is NameOrValue or PropertyAccess or RightCircleBracket;
        }

        protected override And BuildImpl(Range range)
        {
            return new And(range);
        }
    }
}