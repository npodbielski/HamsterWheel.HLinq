using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens.Paging;

/// <summary>
/// The <c>..</c> operator of the <c>take[start..end]</c> range paging syntax. Matches
/// together with the number in front of it (<c>10..</c>)
/// </summary>
public sealed class RangeOperator : TokenBase
{
    // ReSharper disable once UnusedType.Global -> it is used by the Grammar
    public sealed class Word() : TokenWord<RangeOperator>("..")
    {
        protected override TokenPattern BuildPattern(IGrammar grammar) => new RangeOperatorPattern();

        private sealed class RangeOperatorPattern : TokenPattern
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
    }

    /// <summary>
    /// Start of the range as written in the query (<c>take[10 .. 20]</c> gives <c>"10"</c>) or
    /// <c>null</c> for an open start (<c>take[..20]</c>).
    /// </summary>
    public string? GetStartRaw(string query)
    {
        var text = GetValue(query).Trim().TrimEnd('.').Trim();
        return text.Length == 0 ? null : text;
    }

    public static RangeOperator Build(Range range) => new() { Range = range };
    public static RangeOperator Empty { get; } = new() { Range = default };
}