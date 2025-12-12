namespace HamsterWheel.HLinq;

partial class StringExtensions
{
    internal static (bool convertible, object? enumValue) TryParseEnum(this string str, Type type)
    {
        try
        {
            return (true, Enum.Parse(type, str, true));
        }
        catch (Exception)
        {
            Console.WriteLine();
            return (false, null);
        }
    }
}