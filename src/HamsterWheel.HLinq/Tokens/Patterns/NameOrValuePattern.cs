using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     A name or value: unlike <see cref="ValuePattern" /> it may contain any characters a query
///     puts in a value (spaces, dashes, GUIDs, single dots for i.e. <c>1.21</c>) and stays alive
///     until a delimiter ends it. The one text it never holds is a <c>..</c> - that is the
///     <c>RangeOperator</c>, which matches the number in front of it as its own grammar.
/// </summary>
public sealed class NameOrValuePattern(Func<char[]?> delimiters) : TokenPattern
{
    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        for (var dotIndex = 0; dotIndex + 1 < window.Length; dotIndex++)
        {
            if (window[dotIndex] == '.' && window[dotIndex + 1] == '.')
            {
                return MatchAt.Discarded;
            }
        }

        return next is not null && delimiters()?.Contains(next.Value) == true
            ? MatchAt.CompleteAtEnd(window.Length)
            : MatchAt.Possible;
    }
}
