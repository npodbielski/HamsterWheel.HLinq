using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class OrderByPossibility() : TokenPossibility<OrderBy>(TokenValue)
{
    private const string TokenValue = "sortujPo";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.SelectPreviousTokensMatch<OrderBy>(previousTokens);

    protected override OrderBy BuildImpl(Range range) => new(range);
}