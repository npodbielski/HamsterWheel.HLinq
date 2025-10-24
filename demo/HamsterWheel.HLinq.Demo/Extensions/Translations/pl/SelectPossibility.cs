using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class SelectPossibility(IGrammar grammar) : TokenPossibility<Select>(grammar, "wybierz")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);
}