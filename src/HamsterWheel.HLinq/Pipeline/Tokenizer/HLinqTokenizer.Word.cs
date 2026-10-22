using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

partial class HLinqTokenizer
{
    internal sealed record WordTranslation(
        IHLinqWord Word,
        TokenPattern Pattern,
        IGrammarRule Rule,
        Func<Range, IToken> Build);
}