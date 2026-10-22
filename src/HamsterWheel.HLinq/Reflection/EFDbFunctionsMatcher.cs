using System.Reflection;

namespace HamsterWheel.HLinq.Reflection;

public static class EfDbFunctionsMatcher
{
    public static bool IsEfDbFunction(MethodInfo method) => IsEfDbFunction(method.GetParameters());

    public static bool IsEfDbFunction(ParameterInfo[] parameters) =>
        parameters is [{ } first, ..] && IsEfDbFunctionParameter(first);

    public static bool IsEfDbFunctionParameter(ParameterInfo parameter) =>
        parameter is { Name: "_", ParameterType.Name: "DbFunctions" };

    /// <summary>
    /// Get Db function arguments list.
    /// </summary>
    /// <remarks>
    /// All Db functions takes  Microsoft.EntityFrameworkCore.DbFunctions as the first parameter.
    /// This is just a marker and does not have to be bound.
    /// We just remove this parameter from the list.
    /// </remarks>
    public static ParameterInfo[] EfFunctionParametersTransformer(ParameterInfo[] parameters) =>
        IsEfDbFunction(parameters) ? parameters[1..] : parameters;
}
