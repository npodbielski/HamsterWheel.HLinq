using System.Collections.Concurrent;
using System.Reflection;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Reflection;

public sealed class MethodsCache : IMethodsCache
{
    private readonly ConcurrentDictionary<Type, MethodInfo[]> _instanceMethodsCache = new();
    private readonly ConcurrentDictionary<Type, MethodInfo[]> _staticMethodsCache = new();
    private readonly ConcurrentDictionary<(Type, string, bool, Type[]), MethodInfo> _genericMethodsCache = new();

    public MethodInfo[] AllInstance(Type type)
    {
        if (_instanceMethodsCache.TryGetValue(type, out var methods)) return methods;

        return _instanceMethodsCache[type] = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
    }

    public MethodInfo? GetInstance(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector)
    {
        //TODO: C# allows for two members to only be different by case of characters in name i.e. it is perfectly fine to have 'Get' and 'get' methods in type. Probably in this case we should find best suited method via number of params and type of params (probably tricky with strings)
        var methodInfos = AllInstance(type).Where(p => p.Name == method);

        return parameterBasedSelector is not null
            ? methodInfos.SingleOrDefault(m => parameterBasedSelector.Invoke(m.GetParameters()))
            : methodInfos.FirstOrDefault();
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
        GetStatic(type, method, parameterBasedSelector) ?? throw new MissingParseMethodException(type, method);

    public MethodInfo? GetStatic(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector)
    {
        var methodInfos = AllStatic(type).Where(p => p.Name == method);
        return parameterBasedSelector is not null
            ? methodInfos.SingleOrDefault(m => parameterBasedSelector.Invoke(m.GetParameters()))
            : methodInfos.FirstOrDefault();
    }

    public MethodInfo GetStaticGeneric(Type type, string method,
        Func<ParameterInfo[], bool>? parameterBasedSelector = null, params Type[] typeParams) =>
        MakedGeneric(type, method, true, typeParams, parameterBasedSelector);

    public MethodInfo GetInstanceGeneric(Type type, string method,
        Func<ParameterInfo[], bool>? parameterBasedSelector = null, params Type[] typeParams) =>
        MakedGeneric(type, method, false, typeParams, parameterBasedSelector);

    private MethodInfo MakedGeneric(Type type, string name, bool isStatic, Type[] typeParams,
        Func<ParameterInfo[], bool>? parameterBasedSelector = null)
    {
        var tuple = (type, name, isStatic, typeParams);
        if (_genericMethodsCache.TryGetValue(tuple, out var method)) return method;

        if (isStatic)
        {
            method = GetStatic(type, name, parameterBasedSelector) ??
                     throw new ArgumentException($"Could not find static method '{name}' on type '{type.Name}");
        }
        else
        {
            method = GetInstance(type, name, parameterBasedSelector) ??
                     throw new ArgumentException($"Could not find instance method '{name}' on type '{type.Name}");
        }

        return _genericMethodsCache[tuple] = method.MakeGenericMethod(typeParams);
    }

    public sealed class MissingParseMethodException(Type source, string nameOfMethod)
        : HLinqQueryException($"Could not find {nameof(source.Name)}.{nameOfMethod} method");
}