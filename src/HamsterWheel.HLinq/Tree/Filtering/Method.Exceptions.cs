using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using static HamsterWheel.HLinq.Reflection.EfDbFunctionsMatcher;

namespace HamsterWheel.HLinq.Tree.Filtering;

partial class Method
{
    public sealed class InvalidMethodException(Type type, string propName, string method, string[] arguments,
        MethodInfo[] available)
        : HLinqQueryException(
            $"Invalid method for property '{type.Name}.{propName}.{method}'. {ProvidedArgumentsChunk(arguments)}. Available methods at this point are: [{SignaturesChunk(available)}]")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.InvalidMethodParameters;
        public override string InvalidQueryChunk => $"{propName}.{method}";
        public override IReadOnlyList<string> Examples { get; } = Signatures(available);
    }

    public sealed class InvalidStaticMethodException(string method, string[] arguments, MethodInfo[] available)
        : HLinqQueryException(
            $"Invalid static method in query '{method}'. {ProvidedArgumentsChunk(arguments)}. Available methods at this point are: [{SignaturesChunk(available)}]")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.InvalidMethodParameters;
        public override string InvalidQueryChunk => method;
        public override IReadOnlyList<string> Examples { get; } = Signatures(available);
    }

    public sealed class InvalidMethodQueryException(string methodName) : HLinqQueryException(
        $"Could not bound method: '{methodName}' to expression tree in Where root of the query. Make sure method name is correct and it have correct parameters and source.")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.InvalidMethodParameters;
        public override string InvalidQueryChunk => methodName;
    }

    private static string ProvidedArgumentsChunk(string[] arguments) =>
        $"Query provided following argument(s): {string.Join(", ", arguments)}";

    private static string SignaturesChunk(MethodInfo[] candidates) =>
        string.Join(", ", Signatures(candidates));

    private static IReadOnlyList<string> Signatures(MethodInfo[] candidates) =>
        [.. candidates.Select(Signature).Distinct()];

    private static string Signature(MethodInfo candidate)
    {
        var parameters = EfFunctionParametersTransformer(candidate.GetParameters())
            .Select(p => p.ParameterType.Name);

        return $"{candidate.Name}({string.Join(", ", parameters)})";
    }
}
