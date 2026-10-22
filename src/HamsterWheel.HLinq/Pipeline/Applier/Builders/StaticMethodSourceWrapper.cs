using System.Reflection;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public class StaticMethodSourceWrapper(IEnumerable<IStaticMethodSource> childSource) : IStaticMethodSource
{
    public Type[] Types { get; } = [.. childSource.SelectMany(c => c.Types)];

    public static IBindingParameterInfo[] MapEfDbFunctionParams(ParameterInfo[] parameters)
    {
        var list = new List<IBindingParameterInfo>();
        foreach (var info in parameters)
        {
            if (EfDbFunctionsMatcher.IsEfDbFunctionParameter(info))
            {
                list.Add(new BindingParameterInfo
                {
                    Info = info,
                    ConstantValue = null,
                    ConstantType = info.ParameterType
                });
            }
            else
            {
                list.Add(new BindingParameterInfo { Info = info });
            }
        }

        return [.. list];
    }
}