using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class SkipPossibility(IGrammar grammar) : TokenPossibility<Skip>(grammar, TokenValue)
{
    private const string TokenValue = "pomin";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<Skip>(previousTokens);

    protected override Skip BuildImpl(Range range) => new(range);
}