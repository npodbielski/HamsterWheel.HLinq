namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IArithmeticComparisonConditionBuilderContext : IBuilderContext
{
    Type? ComparisonType { get; set; }
}