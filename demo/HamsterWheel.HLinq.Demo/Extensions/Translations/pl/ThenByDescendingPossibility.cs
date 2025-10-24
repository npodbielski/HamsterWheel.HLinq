using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class ThenByDescendingPossibility(IGrammar grammar) : TokenPossibility<ThenByDescending>(grammar, "potemPoMalejaco")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);

    protected override ThenByDescending BuildImpl(Range range) => new(range);
}