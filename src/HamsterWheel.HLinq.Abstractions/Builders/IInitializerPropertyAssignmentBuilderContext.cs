namespace HamsterWheel.HLinq.Builders;

public interface IInitializerPropertyAssignmentBuilderContext : IBuilderContext
{
    public Type? InitializerPropertyType { get; set; }
}