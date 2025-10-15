using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Builders;

public sealed class ConverterFactory : IConverterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private Dictionary<Type, IElementToExpressionConverter>? _expressionConverters;
    private Dictionary<Type, IElementToMemberAssignmentConverter>? _assignmentConverters;

    public ConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _assignmentConverters = serviceProvider.GetServices<IElementToMemberAssignmentConverter>()
            .ToDictionary(c => c.For(), c => c);
    }

    public IElementToExpressionConverter GetToExpressionConverterFor<T>() => GetToExpressionConverterFor(typeof(T));

    public IElementToExpressionConverter GetToExpressionConverterFor(Type type)
    {
        _expressionConverters ??= _serviceProvider.GetServices<IElementToExpressionConverter>()
            .ToDictionary(c => c.For(), c => c);
        return _expressionConverters[type] ?? throw new MissingConverterException(type);
    }

    public IElementToMemberAssignmentConverter GetToMemberAssignmentConverter(Type type)
    {
        _assignmentConverters ??= _serviceProvider.GetServices<IElementToMemberAssignmentConverter>()
            .ToDictionary(c => c.For(), c => c);
        return _assignmentConverters[type] ?? throw new MissingConverterException(type);
    }
}