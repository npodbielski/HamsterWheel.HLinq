using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     A fixed keyword, matched case insensitively, i.e. <c>orderBy</c> or <c>==</c>.
///     While the window is shorter than the keyword, the character after the window must be
///     the next keyword character (case sensitively, so <c>orderBy</c> + <c>D</c> keeps
///     <c>orderByDescending</c> alive and ends <c>orderBy</c>); the token completes only when
///     <paramref name="nextIsAllowed"/> accepts the character after the full match.
/// </summary>
public sealed class KeywordPattern(string keyword, Func<char?, bool>? nextIsAllowed = null) : TokenPattern
{
    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        if (window.Length >= keyword.Length)
        {
            return keyword.AsSpan().Equals(window, StringComparison.OrdinalIgnoreCase) &&
                   (nextIsAllowed?.Invoke(next) ?? true)
                ? MatchAt.CompleteAtEnd(window.Length)
                : MatchAt.Discarded;
        }

        if (!keyword.AsSpan()[..window.Length].Equals(window, StringComparison.OrdinalIgnoreCase))
        {
            return MatchAt.Discarded;
        }

        //i.e., we have window 'order' and the next char is 'b' -> neither 'orderBy' nor 'orderByDescending' can be it
        return next != keyword[window.Length]
            ? MatchAt.Discarded
            : MatchAt.Possible;
    }
}
