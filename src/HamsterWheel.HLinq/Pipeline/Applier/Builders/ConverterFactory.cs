using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed class ConverterFactory(IServiceProvider serviceProvider) : IConverterFactory
{
    private Dictionary<Type, IElementToExpressionConverter>? _expressionConverters;

    private Dictionary<Type, IElementToMemberAssignmentConverter>? _assignmentConverters = serviceProvider
        .GetServices<IElementToMemberAssignmentConverter>()
        .ToSpecializedForsDictionary();

    public IElementToExpressionConverter GetToExpressionConverterFor<T>() => GetToExpressionConverterFor(typeof(T));

    public IElementToExpressionConverter GetToExpressionConverterFor(Type type)
    {
        _expressionConverters ??=
            serviceProvider.GetServices<IElementToExpressionConverter>().ToSpecializedForsDictionary();
        return _expressionConverters.TryGetValue(type, out var converter)
            ? converter
            : throw new MissingConverterException(type);
    }

    public IElementToMemberAssignmentConverter GetToMemberAssignmentConverter(Type type)
    {
        _assignmentConverters ??= serviceProvider.GetServices<IElementToMemberAssignmentConverter>()
            .ToSpecializedForsDictionary();
        return _assignmentConverters.TryGetValue(type, out var converter)
            ? converter
            : throw new MissingConverterException(type);
    }
}