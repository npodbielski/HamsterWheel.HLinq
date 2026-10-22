using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
/// Keyword token pattern. It can be a root 'select' or operation '!='
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
