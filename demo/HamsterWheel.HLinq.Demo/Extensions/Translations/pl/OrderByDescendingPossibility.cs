using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class OrderByDescendingPossibility(IGrammar grammar)
    : TokenPossibility<OrderByDescending>(grammar, "sortujPoMalejaco")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);
}