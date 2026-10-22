using System.Linq.Expressions;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public class ParametersConverter : IParametersConverter
{
    public (bool converted, Expression[] expressions) TryConvert(
        (string value, MemberExpression? expression)[] hLinqParams, IBindingParameterInfo[] parameterTypes)
    {
        var mapped = new List<Expression>(parameterTypes.Length);
        var argument = 0;
        foreach (var binding in parameterTypes)
        {
            if (binding.UseConstant)
            {
                mapped.Add(Expression.Constant(binding.ConstantValue, binding.ConstantType));
                continue;
            }

            //refuse if there are fewer arguments than parameters
            if (argument == hLinqParams.Length)
            {
                return (false, []);
            }

            var filled = MapArgument(hLinqParams[argument], binding.Info.ParameterType);
            if (filled is null)
            {
                return (false, []);
            }

            mapped.Add(filled);
            argument++;
        }

        //refuse if parameter count does not match
        if (argument != hLinqParams.Length)
        {
            return (false, []);
        }

        return (true, [.. mapped]);
    }

    private static Expression? MapArgument((string value, MemberExpression? expression) hLinqParam,
        Type parameterType)
    {
        //property value is just accessed (i.e. method call) so we just need to check if both match
        if (hLinqParam.expression is { } property)
        {
            return parameterType.AcceptsArgumentOfType(property.Type) ? property : null;
        }

        if (parameterType == typeof(string))
        {
            return Expression.Constant(DefaultConverter.Instance.ConvertTo<string>(hLinqParam.value));
        }

        if (parameterType == typeof(char) && hLinqParam.value.Length == 1)
        {
            return Expression.Constant(DefaultConverter.Instance.ConvertTo<char>(hLinqParam.value));
        }

        return parameterType.IsEnum ? MapEnumMember(hLinqParam.value, parameterType) : null;
    }

    private static Expression? MapEnumMember(string value, Type enumType)
    {
        if (!value.Contains('.') && value.TryParseEnum(enumType) is (true, { } enumValue))
        {
            return Expression.Constant(enumValue);
        }

        var splits = value.Split('.');
        if (splits.Length < 2
            || !string.Equals(splits[0], enumType.Name, StringComparison.CurrentCultureIgnoreCase)
            || splits[1].TryParseEnum(enumType) is not (convertible: true, enumValue: { } enumMember))
        {
            return null;
        }

        return Expression.Constant(enumMember);
    }
}
