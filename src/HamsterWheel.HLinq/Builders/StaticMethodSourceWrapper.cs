using System.Reflection;

namespace HamsterWheel.HLinq.Builders;

public class StaticMethodSourceWrapper(IEnumerable<IStaticMethodSource> childSource) : IStaticMethodSource
{
    public Type[] Types { get; } = childSource.SelectMany(c => c.Types).ToArray();

    public static bool IsEfDbFunction(ParameterInfo[] parameters) =>
        parameters is [{ Name: "_", ParameterType.Name: "DbFunctions" }, ..];

    public static IBindingParameterInfo[] MapEfDbFunctionParams(ParameterInfo[] parameters)
    {
        var list = new List<IBindingParameterInfo>();
        foreach (var info in parameters)
        {
            if (info is { Name: "_", ParameterType.Name: "DbFunctions" })
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

        return list.ToArray();
    }
}