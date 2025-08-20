namespace HamsterWheel.HLinq.Tokens.Filter;

public sealed class LeftCircleBracket(Range range) : TokenBase(range)
{
    public const string TokenValue = "(";

    public sealed class Possibility() : TokenPossibility<LeftCircleBracket>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken)
        {
            return previousToken is LeftSquareBracket or And or Or or MethodCall or LeftCircleBracket;
        }

        protected override LeftCircleBracket BuildImpl(Range range)
        {
            return new LeftCircleBracket(range);
        }
    }
}