using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class OrderByPossibility(IGrammar grammar) : TokenPossibility<OrderBy>(grammar, "sortujPo")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);
}