using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class CountPossibility(IGrammar grammar) : TokenPossibility<Count>(grammar, TokenValue)
{
    private const string TokenValue = "zlicz";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<Count>(previousTokens);

    protected override Count BuildImpl(Range range) => new(range);
}