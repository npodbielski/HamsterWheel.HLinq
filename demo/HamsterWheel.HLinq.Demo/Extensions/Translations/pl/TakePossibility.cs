using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class TakePossibility(IGrammar grammar) : TokenPossibility<Take>(grammar, "wez")
{
    protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);
}