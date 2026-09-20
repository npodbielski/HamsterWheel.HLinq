using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Patterns;

/// <summary>
///     The <c>..</c> range operator matched together with the number in front of it
///     (<c>10..</c>, <c>-1..</c>, <c>1.5..</c> or just <c>..</c>), which is how
///     <c>take[10..20]</c> and <c>take[10.2]</c> diverge inside a single tokenizer window
///     without the tokenizer peeking at syntax: at <c>take[10</c> and <c>take[10.</c> both this
///     pattern and NameOrValue are possible, at <c>take[10..</c> only this one is (a NameOrValue
///     never holds <c>..</c>) and it completes, at <c>take[10.2</c> only NameOrValue is left.
///     The operator needs two <b>consecutive</b> dots; single dots keep the window alive as a
///     number prefix (<c>1.5</c> may still grow into <c>1.5..</c>) and everything after the
///     operator (<c>1..2</c>) belongs to the next token, so it discards this window.
/// </summary>
public sealed class RangeOperatorPattern : TokenPattern
{
    public override MatchAt Match(ReadOnlySpan<char> window, char? next)
    {
        var consecutiveDots = 0;
        var firstNotWhiteSpace = true;
        foreach (var c in window)
        {
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            if (consecutiveDots == 2)
            {
                // `1..2` - whatever follows the operator is another token, this window started too early
                return MatchAt.Discarded;
            }

            if (c == '.')
            {
                consecutiveDots++;
            }
            else if (!(char.IsDigit(c) || firstNotWhiteSpace && c == '-'))
            {
                // i.e. a letter - `a..b` is not a range, NameOrValue reads it (and fails later)
                return MatchAt.Discarded;
            }
            else
            {
                consecutiveDots = 0;
            }

            firstNotWhiteSpace = false;
        }

        return consecutiveDots == 2 ? MatchAt.CompleteAtEnd(window.Length) : MatchAt.Possible;
    }
}
