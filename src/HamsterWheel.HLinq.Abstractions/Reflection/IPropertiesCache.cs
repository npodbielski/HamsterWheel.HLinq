using System.Reflection;

namespace HamsterWheel.Common.Reflection;

public interface IPropertiesCache
{
    PropertyInfo[] From<T>();
    PropertyInfo? Single<T>(string prop);
    PropertyInfo[] From(Type type);
    PropertyInfo? Single(Type type, string prop);
}