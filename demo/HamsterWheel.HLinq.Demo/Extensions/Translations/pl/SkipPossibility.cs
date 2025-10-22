using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class SkipPossibility() : TokenPossibility<Skip>(TokenValue)
{
    private const string TokenValue = "pomin";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.SelectPreviousTokensMatch<Skip>(previousTokens);

    protected override Skip BuildImpl(Range range) => new(range);
}