using System.Collections.Concurrent;
using System.Reflection;
using HamsterWheel.Common.Reflection;

namespace HamsterWheel.HLinq.Reflection;

public sealed class PropertiesCache : IPropertiesCache
{
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _cache = new();

    //TODO: we can generate this information during compile time
    //...for what classes exactly? This would be possible to have predefined for entities inside HTTP requests but for `object.ExecuteHLinq` it is not possible
    public PropertyInfo[] From<T>() => From(typeof(T));

    public PropertyInfo[] From(Type type)
    {
        if (_cache.TryGetValue(type, out var props))
        {
            return props;
        }

        return _cache[type] = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
    }

    public PropertyInfo? Single<T>(string prop) => Single(typeof(T), prop);

    public PropertyInfo? Single(Type type, string prop) =>
        From(type).FirstOrDefault(p => p.Name.Equals(prop, StringComparison.InvariantCultureIgnoreCase));
}