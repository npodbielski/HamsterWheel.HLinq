namespace HamsterWheel.HLinq;

internal static class SpecializedForEnumerableExtensions
{
    public static Dictionary<Type, T> ToSpecializedForsDictionary<T>(this IEnumerable<T> collectionOfSpecializedFors)
        where T : ISpecializedFor =>
        collectionOfSpecializedFors.ToDictionary(c => c.For(), c => c);
}