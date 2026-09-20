using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

partial class HLinqTokenizer
{
    public sealed class UnknownTokenException(string queryString, IToken[] expectedTokens)
        : InvalidTokenCollectionException(queryString, [], expectedTokens.Take(1).ToArray(),
            expectedTokens.Skip(1).Select(t => new[] { t }).ToArray());

    public sealed class ZeroLengthCompletionException(string tokenType) : HLinqQueryException(
        $"Token '{tokenType}' reported a match completed at offset 0 of the tokenizer window; " +
        "a token text rule must consume at least one character or the tokenizer walk could not advance.");
}
