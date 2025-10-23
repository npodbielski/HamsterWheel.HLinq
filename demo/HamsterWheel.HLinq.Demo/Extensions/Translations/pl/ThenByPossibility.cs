using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class ThenByPossibility(IGrammar grammar) : TokenPossibility<ThenBy>(grammar, TokenValue)
{
    private const string TokenValue = "potemPo";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<ThenBy>(previousTokens);

    protected override ThenBy BuildImpl(Range range) => new(range);
}