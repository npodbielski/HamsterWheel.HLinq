using System.Reflection;

namespace HamsterWheel.HLinq.Reflection;

public interface IPropertiesCache
{
    PropertyInfo[] From<T>();
    PropertyInfo? Single<T>(string prop);
    PropertyInfo[] From(Type type);
    PropertyInfo? Single(Type type, string prop);
}