using HamsterWheel.HLinq.Tokens;
using static HamsterWheel.HLinq.Pipeline.Tokenizer.Grammar;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class GrammarRootRule<T>() :
    GrammarRule<T>(true, [static pt => pt is [.., RightSquareBracket, Dot]],
        nextCharMatcher: static c => c == LastCharOfLeftSquareBracketKeyword)
    where T : IClauseRootToken;