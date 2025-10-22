using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class ThenByDescendingPossibility() : TokenPossibility<ThenByDescending>(TokenValue)
{
    private const string TokenValue = "potemPoMalejaco";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.SelectPreviousTokensMatch<ThenByDescending>(previousTokens);

    protected override ThenByDescending BuildImpl(Range range) => new(range);
}