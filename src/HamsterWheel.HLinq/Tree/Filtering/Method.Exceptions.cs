using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Filtering;

partial class Method
{
    public sealed class InvalidMethodException(Type type, string propName, string method, string[] available)
        : HLinqQueryException(
            $"Invalid method for property '{type.Name}.{propName}.{method}'. Available methods at this point are: [{string.Join(", ", available)}]");

    public sealed class InvalidStaticMethodException(string method, string[] available)
        : HLinqQueryException(
            $"Invalid static method in query '{method}'. Available methods at this point are: [{string.Join(", ", available)}]");

    private sealed class InvalidMethodQueryException(string methodName) : HLinqQueryException(
        $"Could not bound method: '{methodName}' to expression tree in Where root of the query. Make sure method name is correct and it have correct parameters and source.");
}