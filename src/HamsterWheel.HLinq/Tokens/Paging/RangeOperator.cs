using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Paging;

/// <summary>
///     The <c>..</c> operator of the <c>take[start..end]</c> range paging syntax. Matches
///     together with the number in front of it (<c>10..</c>) - see
///     <see cref="Possibility.CanBeAt" /> - so <see cref="GetStartRaw" /> returns i.e. <c>"10.."</c>.
/// </summary>
public sealed class RangeOperator : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RangeOperator>(grammar, "..")
    {
        // matcher based rule (not AllowedPreviousTokens) on purpose, so '.' does not become
        // a delimiter ending NameOrValue tokens which may legally contain single dots (i.e. `1.21`)
        protected override bool PreviousTokensMatch(List<IToken> previousTokens) =>
            Rule.PreviousTokensMatch(previousTokens);

        /// <summary>
        ///     Matches an optional number (with single dots and an optional leading minus) followed
        ///     by <c>..</c>, i.e. <c>10..</c> or just <c>..</c>. Because the number belongs to this
        ///     token, <c>take[10..20]</c> and <c>take[10.2]</c> can diverge inside a single tokenizer
        ///     window without the tokenizer peeking at syntax: at <c>take[10</c> and <c>take[10.</c>
        ///     both this token and NameOrValue are possible, at <c>take[10..</c> only this one is
        ///     (a NameOrValue never holds <c>..</c>) and it commits, at <c>take[10.2</c> only
        ///     NameOrValue is left.
        /// </summary>
        public override int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
        {
            var dots = 0;
            var firstNotWhiteSpace = true;
            for (var i = 0; i < subset.Length; i++)
            {
                var c = subset[i];
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                if (dots == 2)
                {
                    // `1..2` - whatever follows the operator is another token, this window started too early
                    return 0;
                }

                if (c == '.')
                {
                    dots++;
                }
                else if (!(char.IsDigit(c) || firstNotWhiteSpace && c == '-'))
                {
                    // i.e. a letter - `a..b` is not a range, NameOrValue reads it (and fails later)
                    return 0;
                }

                firstNotWhiteSpace = false;
            }

            if (!PreviousTokensMatch(previousToken))
            {
                return 0;
            }

            return dots == 2 ? 100 : 50;
        }
    }

    /// <summary>
    ///     Start of the range as written in the query (<c>take[10 .. 20]</c> gives <c>"10"</c>) or
    ///     <c>null</c> for an open start (<c>take[..20]</c>).
    /// </summary>
    public string? GetStartRaw(string query)
    {
        var text = GetValue(query).Trim().TrimEnd('.').Trim();
        return text.Length == 0 ? null : text;
    }

    public static RangeOperator Build(Range range) => new() { Range = range };
    public static RangeOperator Empty { get; } = new() { Range = default };
}
