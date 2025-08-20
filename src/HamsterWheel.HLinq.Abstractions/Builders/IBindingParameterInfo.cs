using System.Reflection;

namespace HamsterWheel.HLinq.Builders;

public interface IBindingParameterInfo
{
    ParameterInfo Info { get; init; }
    bool UseConstant { get; }
    object? ConstantValue { get; init; }
    Type ConstantType { get; init; }
}