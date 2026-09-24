using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     A name or value: unlike <see cref="ValuePattern" /> it may contain any characters a query
///     puts in a value (spaces, dashes, GUIDs, single dots for i.e. <c>1.21</c>) and stays alive
///     until a delimiter ends it. The one text it never holds is a <c>..</c> - that is the
///     <c>RangeOperator</c>, which matches the number in front of it as its own grammar.
///     A value that <i>starts</i> with <c>"</c> is quoted: it holds everything until the closing
///     <c>"</c>, delimiters and <c>..</c> included. <c>"a,b"</c> cannot mean anything but one
///     value, because a name is a C# identifier and an identifier holds neither a comma nor a
///     space. Only this text rule knows it - adjacency is untouched, so <c>,</c> stays a
///     delimiter of an unquoted value everywhere, including directly after a closing quote
///     (<c>"a,b",x.Id</c> is one value and one list item). A <c>"</c> that opens inside a value is
///     ordinary text, so the unbalanced quote of <c>say "hi</c> keeps working the way it always
///     did; a quote that opens a value and never closes it never completes, which is what an
///     unclosed value deserves.
/// </summary>
public sealed class NameOrValuePattern(Func<char[]?> delimiters) : TokenPattern
{
    private const char Quote = '"';

    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        var insideQuote = false;
        for (var index = 0; index < window.Length; index++)
        {
            var current = window[index];
            if (current == Quote)
            {
                //a quote opens only at the first character, everywhere else it is text or the close
                if (index == 0)
                {
                    insideQuote = true;
                }
                else if (insideQuote)
                {
                    insideQuote = false;
                }

                continue;
            }

            if (insideQuote)
            {
                //an open quote holds every delimiter, the ones that would end the clause too
                continue;
            }

            if (current == '.' && index + 1 < window.Length && window[index + 1] == '.')
            {
                return MatchAt.Discarded;
            }
        }

        if (insideQuote)
        {
            return MatchAt.Possible;
        }

        return next is not null && delimiters()?.Contains(next.Value) == true
            ? MatchAt.CompleteAtEnd(window.Length)
            : MatchAt.Possible;
    }
}
