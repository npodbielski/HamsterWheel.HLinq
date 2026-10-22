using System.Globalization;
using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

/// <summary>
/// Close value token word. I.e. `select`.
/// </summary>
public class KeywordWord<TToken>(string keyword, CultureInfo? culture = null)
    : TokenWord<TToken>(keyword, culture) where TToken : TokenBase, new()
{
    protected override bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) =>
        !grammar.MatchesLongerKeyword(Keyword!, next);
}
