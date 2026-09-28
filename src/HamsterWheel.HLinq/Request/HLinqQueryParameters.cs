using System.Web;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Request;

/// <summary>
///     A request carries parameters of its own in the query string next to the clauses, and those are
///     the endpoint's to read (<c>[FromQuery]</c> and friends): they go out here and the tokenizer is
///     handed the query alone, so the walk keeps refusing text that is not the query instead of growing
///     a rule for everything a request may carry.
///     Which segments are the query's is answered by the grammar and not guessed here: a segment opening
///     a clause is known by the keywords the grammar registers. The one rule that is not a keyword is
///     that a segment holding a bracket is the query's business too, because brackets belong to the query
///     language - that is what keeps <c>?wher[x.Int&gt;1]&amp;page=2</c> an error instead of a parameter
///     quietly dropped and the whole collection handed back.
///     HLinq reads one parameter itself: the culture, and reading it here is what makes its place in the
///     request meaningless - clauses read with a culture do not care whether the request named it before
///     or after them, and a value in the first clause is read with it either way. A parameter sent more
///     than once is read as the first of its values and the rest are ignored: which of several values a
///     client meant cannot be known here, and a request that already says something is not made an error
///     over it.
///     The string is split the way a url is split, on <c>&amp;</c>, and the pieces are decoded after
///     that: a <c>%26</c> of a value belongs to the value and is not a separator, as it is for the
///     endpoint reading the same request - data that would collide with a delimiter is percent-encoded by
///     whoever writes the url.
/// </summary>
public class HLinqQueryParameters(IEnumerable<IHLinqTokenPossibility> tokenPossibilities,
    ICultureParameterKeyword cultureParameterKeyword) : IHLinqQueryParameters
{
    private const string ParameterSeparator = "&";
    private static readonly char[] ParameterSeparators = ['&'];

    private string[] ClauseRootKeywords => field ??=
        [
            .. tokenPossibilities
                .Where(p => typeof(IClauseRootToken).IsAssignableFrom(p.ForType) && p.Keyword is not null)
                .Select(p => p.Keyword!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

    /// <summary>
    ///     The query, without the parameters of the endpoint, and the culture the request asked for.
    /// </summary>
    public HLinqRequest GetRequest(string queryString)
    {
        List<string> query = [];
        var cultureSent = false;
        string? culture = null;

        foreach (var sent in queryString.Split(ParameterSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = HttpUtility.UrlDecode(sent);
            if (IsCultureParameter(segment, out var value))
            {
                if (!cultureSent)
                {
                    cultureSent = true;
                    culture = value.Length > 0 ? value : null;
                }

                continue;
            }

            if (IsQuery(segment))
            {
                query.Add(segment);
            }
        }

        return new HLinqRequest(string.Join(ParameterSeparator, query), culture);
    }

    private bool IsQuery(string segment) =>
        segment.Contains(LeftSquareBracket.Open)
        || IsClauseRoot(segment);

    private bool IsClauseRoot(string segment) =>
        ClauseRootKeywords.Any(keyword => segment.Equals(keyword, StringComparison.OrdinalIgnoreCase));

    private bool IsCultureParameter(string segment, out string value)
    {
        var keyword = cultureParameterKeyword.Value;
        value = "";

        if (segment.Length <= keyword.Length
            || segment[keyword.Length] != Assignment.Sign
            || !segment.AsSpan(0, keyword.Length)
                .Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = segment[(keyword.Length + 1)..];
        return true;
    }
}
