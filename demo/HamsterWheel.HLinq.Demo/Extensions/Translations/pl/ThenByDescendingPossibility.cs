using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class ThenByDescendingPossibility(IGrammar grammar) : TokenPossibility<ThenByDescending>(grammar, TokenValue)
{
    private const string TokenValue = "potemPoMalejaco";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<ThenByDescending>(previousTokens);

    protected override ThenByDescending BuildImpl(Range range) => new(range);
}