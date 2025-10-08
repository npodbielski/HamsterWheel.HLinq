namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Assignment(Range range) : TokenBase(range), IComparisonToken
{
    public const string TokenValue = "=";

    public sealed class Possibility() : TokenPossibility<Assignment>(TokenValue)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) => previousToken is NameOrValue;

        protected override Assignment BuildImpl(Range range) => new(range);
    }
}