using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class WherePossibility() : TokenPossibility<Where>(TokenValue)
{
    public const string TokenValue = "gdzie";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        GrammarRules.SelectPreviousTokensMatch<Where>(previousTokens);

    protected override Where BuildImpl(Range range) => new(range);
}