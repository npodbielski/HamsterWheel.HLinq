namespace HamsterWheel.HLinq.Builders;

public sealed class ConverterFactory : IConverterFactory
{
    private readonly Dictionary<Type, IElementToExpressionConverter> _expressionConverters;
    private readonly Dictionary<Type, IElementToMemberAssignmentConverter> _assignmentConverters;

    public ConverterFactory(IEnumerable<IElementToExpressionConverter> converters,
        IEnumerable<IElementToMemberAssignmentConverter> assignmentConverters)
    {
        _expressionConverters = converters.ToDictionary(c => c.For(), c => c);
        _assignmentConverters = assignmentConverters.ToDictionary(c => c.For(), c => c);
    }

    public IElementToExpressionConverter GetToExpressionConverterFor(Type type) =>
        _expressionConverters[type] ?? throw new MissingConverterException(type);

    public IElementToMemberAssignmentConverter GetToMemberAssignmentConverter(Type type) =>
        _assignmentConverters[type] ?? throw new MissingConverterException(type);
}