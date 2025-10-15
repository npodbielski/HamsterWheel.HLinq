using System.Text;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Exceptions;

public class InvalidTokenCollectionException(
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

    private static string ShowAlternatives(IToken[] expected, params IToken[][] orExpected)
    {
        var stringBuilder = new StringBuilder();
        if (expected.Length != 0 || orExpected.Length != 0)
        {
            stringBuilder.Append(' ');
        }

        if (expected.Length != 0)
        {
            stringBuilder.Append("Was expecting for example: '");
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
                var line = string.Join("", expectedTokens.Select(t => ExampleOfTokenValue(t, t.GetType())));
                if (index < orExpected.Length - 1)
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

    internal static string ExampleOfTokenValue(IToken token, Type type)
    {
        if (token is TokenExample tokenExample)
        {
            return tokenExample.GetValue("");
        }

        return type.Name switch
        {
            "Select" => "select",
            "Where" => "where",
            "OrderBy" => "orderBy",
            "OrderByDescending" => "orderByDescending",
            "ThenBy" => "thenBy",
            "ThenByDescending" => "thenByDescending",
            "LeftSquareBracket" => "[",
            "RightSquareBracket" => "]",
            "Entity" => "x",
            "Dot" => ".",
            "PropertyAccess" => "Name",
            "Skip" => "skip",
            "Take" => "take",
            "Equality" => "==",
            "NameOrValue" => "Jan",
            "And" => "&&",
            "Or" => "||",
            "MethodCall" => "MethodCall",
            "RightCircleBracket" => ")",
            "LeftCircleBracket" => "(",
            _ => type.Name
        };
    }
}