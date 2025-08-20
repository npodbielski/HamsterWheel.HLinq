namespace HamsterWheel.HLinq;

public static class ArrayExtensions
{
    public static IEnumerable<T> GetFirstItems<T>(this T[] array, int number) =>
        array.Length < number ? array : array.Take(number);
}