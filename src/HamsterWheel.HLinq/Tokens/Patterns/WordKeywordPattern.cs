using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     A keyword that is also a word, i.e. the entity <c>x</c>: it completes only when the character
///     after it is one the grammar allows to follow it (its derived delimiters), not as soon as its
///     own characters are matched. A keyword allowed next to a free text token would otherwise win
///     over the longer text starting with the same character, so <c>x.Name==xander</c> would be read
///     as the entity <c>x</c> followed by junk instead of one value.
/// </summary>
public sealed class WordKeywordPattern(string keyword, Func<char[]?> delimiters) : TokenPattern
{
    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        if (window.Length > keyword.Length ||
            !keyword.AsSpan(0, window.Length).Equals(window, StringComparison.OrdinalIgnoreCase))
        {
            return MatchAt.Discarded;
        }

        if (window.Length < keyword.Length)
        {
            return next == keyword[window.Length]
                ? MatchAt.Possible
                : MatchAt.Discarded;
        }

        if (next is null)
        {
            return MatchAt.CompleteAtEnd(window.Length);
        }

        return delimiters()?.Contains(next.Value) == true
            ? MatchAt.CompleteAtEnd(window.Length)
            : MatchAt.Discarded;
    }
}
