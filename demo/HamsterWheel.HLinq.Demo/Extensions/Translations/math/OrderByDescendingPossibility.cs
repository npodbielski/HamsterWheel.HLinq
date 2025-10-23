using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.math;

public class OrderByDescendingPossibility(IGrammar grammar) : TokenPossibility<OrderByDescending>(grammar, TokenValue)
{
    private const string TokenValue = "↓";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<OrderByDescending>(previousTokens);

    protected override OrderByDescending BuildImpl(Range range) => new(range);
}