using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class ThenByPossibility() : TokenPossibility<ThenBy>(TokenValue)
{
    private const string TokenValue = "potemPo";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.PreviousTokensMatch<ThenBy>(previousTokens);

    protected override ThenBy BuildImpl(Range range) => new(range);
}