namespace HamsterWheel.HLinq;

partial class StringArrayExtensions
{
    public static string JoinWithDot(this string[] array) => string.Join('.', array);
}