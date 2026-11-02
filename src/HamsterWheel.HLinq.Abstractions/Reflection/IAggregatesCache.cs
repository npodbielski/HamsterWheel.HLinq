using System.Reflection;

namespace HamsterWheel.HLinq.Reflection;

/// <summary>
/// Which <see cref="Enumerable"/> methods a query may call as aggregates, read from Enumerable's
/// own signatures once - a clause should not pay for that reflection again on every query.
/// </summary>
public interface IAggregatesCache
{
    /// <summary>
    /// The names a query may name, for a refusal that lists them.
    /// </summary>
    IReadOnlyList<string> Names();

    bool IsAggregateName(string name);

    /// <summary>
    /// The items of a collection type - the rows of a group, the members of a list; null when the
    /// type is no collection.
    /// </summary>
    Type? ItemOf(Type source);

    /// <summary>
    /// Whether values of this type compare to each other.
    /// </summary>
    bool IsComparable(Type item);

    /// <summary>
    /// The overload of this name bound to an item type and, when the aggregate reads a member, a
    /// value type: null when no overload binds. The flag says a method of the bare Min/Max shape
    /// was found but its items do not compare to each other.
    /// </summary>
    (MethodInfo? Method, bool NeedsComparableItems) Resolve(string name, Type item, Type? value);
}
