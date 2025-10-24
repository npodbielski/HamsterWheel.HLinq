namespace HamsterWheel.HLinq.Builders;

public interface IArithmeticComparisonConditionBuilderContext : IBuilderContext
{
    Type? ComparisonType { get; set; }
}