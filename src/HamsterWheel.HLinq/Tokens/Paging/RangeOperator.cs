using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens.Paging;

/// <summary>
///     The <c>..</c> operator of the <c>take[start..end]</c> range paging syntax. Matches
///     together with the number in front of it (<c>10..</c>) - see
///     <see cref="Possibility.MatchesPreviousTokens" /> - so <see cref="GetStartRaw" /> returns i.e. <c>"10.."</c>.
/// </summary>
public sealed class RangeOperator : TokenBase
{
    public sealed class Possibility(IGrammar grammar) : TokenPossibility<RangeOperator>(grammar, "..")
    {
        // matcher based rule (not AllowedPreviousTokens) on purpose, so '.' does not become
        // a delimiter ending NameOrValue tokens which may legally contain single dots (i.e. `1.21`)

        protected override TokenPattern BuildPattern() => new RangeOperatorPattern();
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
