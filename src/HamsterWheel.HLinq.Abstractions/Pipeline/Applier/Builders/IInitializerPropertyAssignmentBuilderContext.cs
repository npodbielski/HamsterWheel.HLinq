namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IInitializerPropertyAssignmentBuilderContext : IBuilderContext
{
    public Type? InitializerPropertyType { get; set; }
}