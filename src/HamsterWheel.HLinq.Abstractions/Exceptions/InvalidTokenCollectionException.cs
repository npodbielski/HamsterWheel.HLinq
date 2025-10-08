using System.Text;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Exceptions;

public sealed class InvalidTokenCollectionException(
    string queryString,
    IToken[] actual,
    IToken[] expected,
    params IToken[][] orExpected)
    : HLinqQueryException(
        $"HLinq query '{queryString}' is invalid {ShowInvalidPart(actual, queryString)}.{ShowAlternatives(expected, orExpected)}")
{
    private static string ShowInvalidPart(IToken[] actual, string queryString) =>
        actual.Length > 0
            ? $"at character {actual.First().Range.Start}: '{string.Join("", actual.Select(a => a.GetValue(queryString)))}'"
            : "and not finished properly";

    private static string ShowAlternatives(IToken[] expected,
        params IToken[][] orExpected)
    {
        var stringBuilder = new StringBuilder();
        if (expected.Length != 0 || orExpected.Length != 0)
        {
            stringBuilder.Append(" ");
        }

        if (expected.Length != 0)
        {
            stringBuilder.Append("Was expecting: '");
            stringBuilder.Append(string.Join("", expected.Select(t => ExampleOfTokenValue(t, t.GetType()))));
            stringBuilder.Append("'.");
        }

        if (orExpected.Length != 0)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("You can also try:");

            for (var index = 0; index < orExpected.Length; index++)
            {
                var expectedTokens = orExpected[index];
                stringBuilder.Append(" - ");
                var line = string.Join(",", expectedTokens.Select(t => ExampleOfTokenValue(t, t.GetType())));
                if (index <= expectedTokens.Length)
                {
                    stringBuilder.AppendLine(line);
                }
                else
                {
                    stringBuilder.Append(line);
                }
            }
        }

        return stringBuilder.ToString();
    }

    private static string ExampleOfTokenValue(IToken token, Type type)
    {
        if (token is TokenExample tokenExample)
        {
            return tokenExample.GetValue("");
        }

        switch (type.Name)
        {
            case "Select":
                return "select";
            case "Where":
                return "where";
            case "OrderBy":
                return "orderby";
            case "OrderByDescending":
                return "orderbyDescending";
            case "ThenBy":
                return "thenBy";
            case "ThenByDescending":
                return "thenByDescending";
            case "LeftSquareBracket":
                return "[";
            case "RightSquareBracket":
                return "]";
            case "Entity":
                return "x";
            case "Dot":
                return ".";
            case "PropertyAccess":
                return "Name";
            case "Skip":
                return "skip";
            case "Take":
                return "take";
            case "Equality":
                return "==";
            case "NameOrValue":
                return "Jan";
            case "And":
                return "&&";
            case "Or":
                return "||";
            case "MethodCall":
                return "MethodCall";
            case "RightCircleBracket":
                return ")";
            default:
                return type.Name;
        }
    }
}