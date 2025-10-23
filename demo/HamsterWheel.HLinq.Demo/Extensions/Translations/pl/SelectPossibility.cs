using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class SelectPossibility(IGrammar grammar) : TokenPossibility<Select>(grammar, TokenValue)
{
    private const string TokenValue = "wybierz";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<Select>(previousTokens);

    protected override Select BuildImpl(Range range) => new(range);
}