namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Or(Range range) : TokenBase(range), IConditionalLogicalOperationToken
{
    public const string TokenValue = "||";

    public sealed class Possibility() : TokenPossibility<Or>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is NameOrValue or PropertyAccess or RightCircleBracket;

        protected override Or BuildImpl(Range range) => new(range);
    }
}