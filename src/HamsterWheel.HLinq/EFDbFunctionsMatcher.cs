using System.Reflection;

namespace HamsterWheel.HLinq;

public static class EfDbFunctionsMatcher
{
    public static bool IsEfDbFunction(MethodInfo method) => IsEfDbFunction(method.GetParameters());

    public static bool IsEfDbFunction(ParameterInfo[] parameters) =>
        parameters is [{ } first, ..] && IsEfDbFunctionParameter(first);

    public static bool IsEfDbFunctionParameter(ParameterInfo parameter) =>
        parameter is { Name: "_", ParameterType.Name: "DbFunctions" };
}