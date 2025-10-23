using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class CountPossibility() : TokenPossibility<Count>(TokenValue)
{
    private const string TokenValue = "zlicz";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.PreviousTokensMatch<Count>(previousTokens);

    protected override Count BuildImpl(Range range) => new(range);
}