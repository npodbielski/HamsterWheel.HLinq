using System.Collections.Concurrent;
using System.Reflection;

namespace HamsterWheel.HLinq.Reflection;

public sealed class PropertiesCache : IPropertiesCache
{
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _cache = new();

    public PropertyInfo[] From(Type type)
    {
        if (_cache.TryGetValue(type, out var props))
        {
            return props;
        }

        return _cache[type] = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
    }

    public PropertyInfo? Single(Type type, string prop) =>
        From(type).FirstOrDefault(p => p.Name.Equals(prop, StringComparison.InvariantCultureIgnoreCase));
}