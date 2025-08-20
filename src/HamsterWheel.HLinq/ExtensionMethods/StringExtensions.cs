namespace HamsterWheel.HLinq;

internal static class StringExtensions
{
    internal static string ToCamelCase(this string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        var firstLetter = str[0].ToString();
        return $"{firstLetter.ToLower()}{str[1..]}";
    }

    public static string Quote(this string str) => $"\"{str}\"";

    internal static string TakeAtMost(this string str, int length) => str.Length > length ? str[..length] : str;
}