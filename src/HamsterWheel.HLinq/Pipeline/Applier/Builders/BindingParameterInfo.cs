using System.Reflection;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public class BindingParameterInfo : IBindingParameterInfo
{
    public ParameterInfo Info { get; init; } = null!;

    public bool UseConstant { get; private init; }

    public object? ConstantValue
    {
        get;
        init
        {
            field = value;
            UseConstant = true;
        }
    }

    public Type ConstantType { get; init; } = null!;
}