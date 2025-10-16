namespace HamsterWheel.HLinq.Tokens.Selecting;

public sealed class Select(Range range) : TokenBase(range)
{
    public sealed class Possibility() : TokenPossibility<Select>(TokenValue)
    {
        public const string TokenValue = "select";

        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            GrammarRules.SelectPreviousTokensMatch<Select>(previousTokens);

        protected override Select BuildImpl(Range range) => new(range);
    }
}