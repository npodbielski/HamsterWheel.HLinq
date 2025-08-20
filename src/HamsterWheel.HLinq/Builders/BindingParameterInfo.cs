using System.Reflection;

namespace HamsterWheel.HLinq.Builders;

public class BindingParameterInfo : IBindingParameterInfo
{
    private readonly object? _constantValue;
    public ParameterInfo Info { get; init; } = null!;

    public bool UseConstant { get; private init; }

    public object? ConstantValue
    {
        get => _constantValue;
        init
        {
            _constantValue = value;
            UseConstant = true;
        }
    }

    public Type ConstantType { get; init; } = null!;
}