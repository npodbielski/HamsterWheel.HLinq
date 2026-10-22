using System.Reflection;

namespace HamsterWheel.HLinq.Reflection;

public interface IPropertiesCache
{
    PropertyInfo[] From(Type type);

    PropertyInfo? Single(Type type, string prop);

    MemberResolution<PropertyInfo> Resolve(Type type, string prop);
}