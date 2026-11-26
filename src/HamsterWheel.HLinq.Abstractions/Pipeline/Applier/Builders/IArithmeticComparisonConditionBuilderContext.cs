using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IArithmeticComparisonConditionBuilderContext : IBuilderContext
{
    Type? ComparisonType { get; set; }

    /// <summary>
    /// Reads a value the query wrote into the compared type.
    /// </summary>
    IDefaultConverter Converter { get; }
}