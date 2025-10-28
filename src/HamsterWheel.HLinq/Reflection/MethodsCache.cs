using System.Collections.Concurrent;
using System.Reflection;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Reflection;

public sealed class MethodsCache : IMethodsCache
{
    private readonly ConcurrentDictionary<Type, MethodInfo[]> _instanceMethodsCache = new();
    private readonly ConcurrentDictionary<Type, MethodInfo[]> _staticMethodsCache = new();
    private readonly ConcurrentDictionary<(Type, string, Type[]), MethodInfo> _genericMethodsCache = new();

    public MethodInfo[] AllInstance(Type type)
    {
        if (_instanceMethodsCache.TryGetValue(type, out var methods)) return methods;

        return _instanceMethodsCache[type] = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
    }

    public MethodInfo[] AllStatic(Type type)
    {
        if (_staticMethodsCache.TryGetValue(type, out var methods))
        {
            return methods;
        }

        return _staticMethodsCache[type] =
        [
            ..type.GetMethods(BindingFlags.Static | BindingFlags.Public),
            ..type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        ];
    }

    public MethodInfo GetStaticOrThrow(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector) =>
        GetStatic(type, method, parameterBasedSelector) ?? throw new MissingStaticMethodException(type, method);

    public MethodInfo? GetStatic(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector)
    {
        var methodInfos = AllStatic(type).Where(p => p.Name == method);
        return parameterBasedSelector is not null
            ? methodInfos.SingleOrDefault(m => parameterBasedSelector.Invoke(m.GetParameters()))
            : methodInfos.FirstOrDefault();
    }

    public MethodInfo GetStaticGeneric(Type type, string method,
        Func<ParameterInfo[], bool>? parameterBasedSelector = null, params Type[] typeParams)
    {
        var tuple = (type, method, typeParams);
        if (_genericMethodsCache.TryGetValue(tuple, out var method1))
        {
            return method1;
        }

        method1 = GetStatic(type, method, parameterBasedSelector) ??
                  throw new ArgumentException($"Could not find static method '{method}' on type '{type.Name}");

        return _genericMethodsCache[tuple] = method1.MakeGenericMethod(typeParams);
    }

    public sealed class MissingStaticMethodException(Type source, string nameOfMethod)
        : HLinqQueryException($"Could not find static {nameof(source.Name)}.{nameOfMethod} method");
}