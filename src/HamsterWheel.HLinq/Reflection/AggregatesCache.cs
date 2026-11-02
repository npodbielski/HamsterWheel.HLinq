using System.Collections.Concurrent;
using System.Reflection;
using static System.StringComparison;

namespace HamsterWheel.HLinq.Reflection;

/// <summary>
/// Reads Enumerable's own signatures once: a method is an aggregate when it takes a collection
/// as the first parameter and optionally a selector as the second, and answers a single value.
/// </summary>
public sealed class AggregatesCache : IAggregatesCache
{
    private readonly ConcurrentDictionary<string, MethodInfo[]> _bare = new();
    private readonly ConcurrentDictionary<string, MethodInfo[]> _withSelector = new();
    private readonly ConcurrentDictionary<(string, Type, Type?), (MethodInfo? Method, bool NeedsComparableItems)> _resolved = new();
    private readonly ConcurrentDictionary<Type, Type?> _items = new();
    private readonly ConcurrentDictionary<Type, bool> _comparable = new();

    private readonly Lazy<HashSet<string>> _names;
    private readonly Lazy<IReadOnlyList<string>> _sortedNames;

    public AggregatesCache()
    {
        var aggregateNames = new Lazy<IReadOnlyList<string>>(() =>
            [.. typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.GetParameters() is [{ } first]
                            && IsSource(first.ParameterType)
                            && !IsCollection(m.ReturnType)
                            && (m.GetParameters().Length == 1
                                || m.GetParameters().Length == 2 && IsSelector(m.GetParameters()[1].ParameterType)))
                .Select(m => m.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)]);

        _names = new Lazy<HashSet<string>>(() => new HashSet<string>(aggregateNames.Value, StringComparer.OrdinalIgnoreCase));
        _sortedNames = new Lazy<IReadOnlyList<string>>(() => aggregateNames.Value);
    }

    public IReadOnlyList<string> Names() => _sortedNames.Value;

    public bool IsAggregateName(string name) => _names.Value.Contains(name);

    public Type? ItemOf(Type source) => _items.GetOrAdd(source, ItemOfImpl);

    public bool IsComparable(Type item) => _comparable.GetOrAdd(item, static t =>
        t.GetInterfaces().Concat(t.IsInterface ? [t] : [])
            .Any(i => i == typeof(IComparable) || (i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IComparable<>)
                && (i.GenericTypeArguments[0] == t
                    || i.GenericTypeArguments[0] == Nullable.GetUnderlyingType(t)))));

    public (MethodInfo? Method, bool NeedsComparableItems) Resolve(string name, Type item, Type? value)
    {
        if (_resolved.TryGetValue((name, item, value), out var bound))
        {
            return bound;
        }

        var wantsComparable = false;
        foreach (var candidate in value is null ? Bare(name) : WithSelector(name))
        {
            try
            {
                var made = candidate.GetGenericArguments().Length switch
                {
                    0 => candidate,
                    1 => candidate.MakeGenericMethod(item),
                    2 when value is not null => candidate.MakeGenericMethod(item, value),
                    _ => null
                };

                if (made is null || !Binds(made, item, value))
                {
                    continue;
                }

                //Enumerable.Min/Max does not limit types it can run against, so we need to check
                //if type implements IComparable or similar - a call answering one item of its
                //own collection is that shape
                if (made.ReturnType != item || IsComparable(item))
                {
                    return _resolved[(name, item, value)] = (made, false);
                }
                wantsComparable = true;
            }
            catch (ArgumentException)
            {
                //if method cannot be bound
            }
        }

        return _resolved[(name, item, value)] = (null, wantsComparable);
    }

    private MethodInfo[] Bare(string name) => _bare.GetOrAdd(name, n =>
    [
        .. typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name.Equals(n, CurrentCultureIgnoreCase)
                        && m.GetParameters().Length == 1
                        && IsSource(m.GetParameters()[0].ParameterType)
                        && !IsCollection(m.ReturnType))
    ]);

    private MethodInfo[] WithSelector(string name) => _withSelector.GetOrAdd(name, n =>
    [
        .. typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name.Equals(n, CurrentCultureIgnoreCase)
                        && m.GetParameters().Length == 2
                        && IsSource(m.GetParameters()[0].ParameterType)
                        && IsSelector(m.GetParameters()[1].ParameterType)
                        && !IsCollection(m.ReturnType))
    ]);

    private static bool Binds(MethodInfo made, Type item, Type? value)
    {
        //a non-generic numeric overload binds only when the items are exactly its own kind of
        //value (Max(IEnumerable<int>) of a list of decimals binds nothing)
        var sourceItem = made.GetParameters()[0].ParameterType.GetGenericArguments()[0];
        if (sourceItem != item)
        {
            return false;
        }

        if (value is null)
        {
            return true;
        }

        var selectorType = made.GetParameters()[1].ParameterType;
        return selectorType.GetGenericArguments()[1] is var kind
               && (kind == value || Nullable.GetUnderlyingType(kind) == value);
    }

    private static Type? ItemOfImpl(Type source)
    {
        var grouping = source.GetInterfaces()
            .Concat(source.IsInterface ? [source] : [])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGrouping<,>));
        if (grouping is not null)
        {
            return grouping.GenericTypeArguments[1];
        }

        //a string is deliberately no collection here, there is nothing to count of a name
        if (typeof(IEnumerable<char>).IsAssignableFrom(source))
        {
            return null;
        }

        var enumerable = source.GetInterfaces()
            .Concat(source.IsInterface ? [source] : [])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GenericTypeArguments[0];
    }

    private static bool IsSource(Type parameter) =>
        parameter.IsGenericType && parameter.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    private static bool IsSelector(Type parameter) =>
        parameter.IsGenericType && parameter.GetGenericTypeDefinition() == typeof(Func<,>);

    private static bool IsCollection(Type returnType) =>
        typeof(System.Collections.IEnumerable).IsAssignableFrom(returnType) && returnType != typeof(string);
}