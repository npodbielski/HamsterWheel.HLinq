using System.Collections.Concurrent;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

partial class HLinqTokenizer
{
    public sealed class UnknownTokenException(string queryString, IToken[] expectedTokens)
        : InvalidTokenCollectionException(queryString, [], [.. expectedTokens.Take(1)],
            [.. expectedTokens.Skip(1).Select(t => new[] { t })]);

    public class UnknownTokenTypeException(Type tokenType)
        : HLinqQueryException(
            $"Unknown token type: '{tokenType.Name}'. This token is not supported by HLinq grammar");

    public sealed class ZeroLengthCompletionException(string tokenType) : HLinqQueryException(
        $"Token '{tokenType}' reported a match completed at offset 0 of the tokenizer window; " +
        "a token text rule must consume at least one character or the tokenizer could not advance.");
}
