using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class WherePossibility(IGrammar grammar) : TokenPossibility<Where>(grammar, TokenValue)
{
    private const string TokenValue = "gdzie";

    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Grammar.PreviousTokensMatch<Where>(previousTokens);

    protected override Where BuildImpl(Range range) => new(range);
}