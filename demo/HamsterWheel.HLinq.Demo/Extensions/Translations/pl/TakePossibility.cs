using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class TakePossibility() : TokenPossibility<Take>(TokenValue)
{
    private const string TokenValue = "wez";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.SelectPreviousTokensMatch<Take>(previousTokens);

    protected override Take BuildImpl(Range range) => new(range);
}