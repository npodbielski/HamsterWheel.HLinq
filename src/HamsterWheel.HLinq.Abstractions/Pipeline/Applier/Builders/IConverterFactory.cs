namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IConverterFactory
{
    IElementToExpressionConverter GetToExpressionConverterFor(Type type);
    IElementToMemberAssignmentConverter GetToMemberAssignmentConverter(Type type);
    IElementToExpressionConverter GetToExpressionConverterFor<T>();
}