using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class CountPossibility(IGrammar grammar) : TokenPossibility<Count>(grammar, "zlicz")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);

    protected override Count BuildImpl(Range range) => new(range);
}