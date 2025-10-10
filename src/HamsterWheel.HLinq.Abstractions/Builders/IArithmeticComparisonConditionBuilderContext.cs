namespace HamsterWheel.HLinq.Builders;

public interface IArithmeticComparisonConditionBuilderContext : IBuilderContext
{
    Type? ComparisonPropertyType { get; set; }
}