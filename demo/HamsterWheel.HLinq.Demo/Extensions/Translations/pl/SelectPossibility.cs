using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class SelectPossibility() : TokenPossibility<Select>(TokenValue)
{
    private const string TokenValue = "wybierz";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.PreviousTokensMatch<Select>(previousTokens);

    protected override Select BuildImpl(Range range) => new(range);
}