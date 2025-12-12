namespace HamsterWheel.HLinq;

internal static partial class StringExtensions
{
    public static string Quote(this string str) => $"\"{str}\"";
    public static bool IsDoubleQuoted(this string str) => str is ['"', .., '"'];
    public static bool IsSingleQuoted(this string str) => str is ['\'', .., '\''];
    public static string UnQuote(this string str) => str[1..^1];

    internal static string TakeAtMost(this string str, int length) => str.Length > length ? str[..length] : str;

    internal static string ToCamelCase(this string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        var firstLetter = str[0].ToString();
        return $"{firstLetter.ToLower()}{str[1..]}";
    }

    internal static bool StartsWith(this string str, char c) => str.StartsWith(c.ToString());
}