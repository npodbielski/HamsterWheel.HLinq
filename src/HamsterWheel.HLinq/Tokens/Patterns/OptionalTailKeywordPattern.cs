using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     A keyword that also matches with its last character left out, i.e. <c>==</c> also matching
///     <c>=</c>: while the character after the window could still grow it into the full keyword the
///     window stays possible, and only once it cannot does the shorter form complete. Both spellings
///     produce the same token, so a query never has to guess which one its author meant and the
///     longer one is never cut in half.
/// </summary>
public sealed class OptionalTailKeywordPattern(string keyword) : TokenPattern
{
    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        if (window.Length is 0 || window.Length > keyword.Length ||
            !keyword.AsSpan(0, window.Length).Equals(window, StringComparison.OrdinalIgnoreCase))
        {
            return MatchAt.Discarded;
        }

        if (window.Length == keyword.Length)
        {
            //a character that would start the tail again is nothing this token can be made of
            return next != keyword[^1]
                ? MatchAt.CompleteAtEnd(window.Length)
                : MatchAt.Discarded;
        }

        if (next == keyword[window.Length])
        {
            return MatchAt.Possible;
        }

        return next is null || window.Length == keyword.Length - 1
            ? MatchAt.CompleteAtEnd(window.Length)
            : MatchAt.Discarded;
    }
}
