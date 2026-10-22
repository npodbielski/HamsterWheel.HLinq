using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class GrammarRootRule<T>(char[] delimiters) :
    GrammarRule<T>(true, [static pt => pt is [.., RightSquareBracket, Dot]], nextCharsAllowed: delimiters)
    where T : IClauseRootToken;
