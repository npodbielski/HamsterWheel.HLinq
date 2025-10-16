namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class Or(Range range) : TokenBase(range), ILogicalOperatorToken
{
    public sealed class Possibility() : TokenPossibility<Or>(TokenValue)
    {
        public const string TokenValue = "||";
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            previousToken is NameOrValue or PropertyAccess or RightCircleBracket;

        protected override Or BuildImpl(Range range) => new(range);
    }
}