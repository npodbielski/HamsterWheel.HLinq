using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Demo.Extensions.Translations.pl;

public class WherePossibility(IGrammar grammar) : TokenPossibility<Where>(grammar, "gdzie")
{
}