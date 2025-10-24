using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class WherePossibility(IGrammar grammar) : TokenPossibility<Where>(grammar, "gdzie")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);
}