using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class OrderByDescendingPossibility() : TokenPossibility<OrderByDescending>(TokenValue)
{
    private const string TokenValue = "sortujPoMalejaco";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.PreviousTokensMatch<OrderByDescending>(previousTokens);

    protected override OrderByDescending BuildImpl(Range range) => new(range);
}