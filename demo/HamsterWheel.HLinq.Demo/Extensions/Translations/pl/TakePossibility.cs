using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class TakePossibility(IGrammar grammar) : TokenPossibility<Take>(grammar, TokenValue)
{
    private const string TokenValue = "wez";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<Take>(previousTokens);

    protected override Take BuildImpl(Range range) => new(range);
}