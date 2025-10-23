using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class OrderByPossibility(IGrammar grammar) : TokenPossibility<OrderBy>(grammar, TokenValue)
{
    private const string TokenValue = "sortujPo";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<OrderBy>(previousTokens);

    protected override OrderBy BuildImpl(Range range) => new(range);
}