using System.Web;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Request;

/// <summary>
/// Extracts HLinq query from the entire Query string
/// </summary>
public class HLinqQueryParameters(
    IEnumerable<IHLinqWord> tokenWords,
    ICultureParameterKeyword cultureParameterKeyword) : IHLinqQueryParameters
{
    private const string ParameterSeparator = "&";

    private string[] ClauseRootKeywords => field ??=
    [
        .. tokenWords
            .Where(p => typeof(IClauseRootToken).IsAssignableFrom(p.ForType) && p.Keyword is not null)
            .Select(p => p.Keyword!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
    ];

    public HLinqRequest GetRequest(string queryString)
    {
        List<string> query = [];
        var cultureSent = false;
        string? culture = null;

        foreach (var sent in Separators(queryString))
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

    private static IEnumerable<string> Separators(string queryString)
    {
        var clauseDepth = 0;
        var insideQuote = false;
        var start = 0;

        for (var position = 0; position < queryString.Length; position++)
        {
            switch (queryString[position])
            {
                case '"':
                    insideQuote = !insideQuote;
                    break;
                case '[' when !insideQuote:
                    clauseDepth++;
                    break;
                case ']' when !insideQuote:
                    clauseDepth = Math.Max(0, clauseDepth - 1);
                    break;
                case '&' when !insideQuote && clauseDepth == 0:
                    yield return queryString[start..position];
                    start = position + 1;
                    break;
            }
        }

        yield return queryString[start..];
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