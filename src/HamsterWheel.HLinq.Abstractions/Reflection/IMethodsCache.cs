using System.Reflection;

namespace HamsterWheel.HLinq.Reflection;

public interface IMethodsCache
{
    MethodInfo? GetInstance(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector = null);
    MethodInfo[] AllInstance(Type type);
    MethodInfo[] AllStatic(Type type);
    MethodInfo? GetStatic(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector);

    MethodInfo GetStaticGeneric(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector = null,
        params Type[] typeParams);

    MethodInfo GetInstanceGeneric(Type type, string method, Func<ParameterInfo[], bool>? parameterBasedSelector = null,
        params Type[] typeParams);
}