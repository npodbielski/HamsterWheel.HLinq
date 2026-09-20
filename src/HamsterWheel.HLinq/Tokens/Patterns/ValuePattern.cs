using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     Free text ending on a delimiter, i.e. <c>Entity</c> or <c>PropertyName</c>: completes
///     when the next character is one of the <paramref name="delimiters" />, dies on any other
///     character which cannot continue the text (not a letter, digit or underscore), and stays
///     possible otherwise.
/// </summary>
public class ValuePattern(Func<char[]?> delimiters) : TokenPattern
{
    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        if (next is null)
        {
            return MatchAt.Possible;
        }

        if (delimiters()?.Contains(next.Value) == true)
        {
            return MatchAt.CompleteAtEnd(window.Length);
        }

        return !char.IsLetterOrDigit(next.Value) && next.Value != '_' ? MatchAt.Discarded : MatchAt.Possible;
    }
}
