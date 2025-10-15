using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.UnitTests;

public static class IHLinqParserTestExtensions
{
    public static HLinqQuery<T> TestParseEntryPoint<T>(this IHLinqParser parser, string query, IToken[] tokens)
        where T : class
    {
        var hlinqQuery = new HLinqQuery<T>
        {
            SourceQueryString = query
        };
        return (HLinqQuery<T>)parser.Parse(hlinqQuery, tokens);
    }
}