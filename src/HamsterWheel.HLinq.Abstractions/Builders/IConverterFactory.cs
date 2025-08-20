namespace HamsterWheel.HLinq.Builders;

public interface IConverterFactory
{
    IElementToExpressionConverter GetToExpressionConverterFor(Type type);
    IElementToMemberAssignmentConverter GetToMemberAssignmentConverter(Type type);
}