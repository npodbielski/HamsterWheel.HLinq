using System.Linq.Expressions;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public class ParametersConverter : IParametersConverter
{
    public Expression[] AttemptConversion((string value, MemberExpression? expression)[] hLinqParams,
        IBindingParameterInfo[] parameterTypes)
    {
        var expressions = new List<Expression>();
        var i = 0;
        foreach (var bindingParameterInfo in parameterTypes)
        {
            if (bindingParameterInfo.UseConstant)
            {
                expressions.Add(Expression.Constant(bindingParameterInfo.ConstantValue,
                    bindingParameterInfo.ConstantType));
                continue;
            }

            var type = bindingParameterInfo.Info.ParameterType;
            var param = hLinqParams[i];
            if (type == typeof(string))
            {
                expressions.Add(param.expression is not null
                    ? param.expression
                    : Expression.Constant(DefaultConverter.Instance.ConvertTo<string>(param.value)));
                i++;
                continue;
            }
            if (type == typeof(char) && param.value.Length == 1)
            {
                expressions.Add(param.expression is not null
                    ? param.expression
                    : Expression.Constant(DefaultConverter.Instance.ConvertTo<char>(param.value)));
                i++;
                continue;
            }

            if (!type.IsEnum)
            {
                continue;
            }

            if (!param.value.Contains('.') && type.IsEnum && param.value.TryParseEnum(type) is (true, { } enumValue))
            {
                expressions.Add(Expression.Constant(enumValue));
                i++;
                continue;
            }

            var splits = param.value.Split('.');
            if (!string.Equals(splits[0], type.Name, StringComparison.CurrentCultureIgnoreCase)
                || splits[1].TryParseEnum(type) is not (convertible: true, enumValue: { } enumMember))
            {
                continue;
            }

            expressions.Add(Expression.Constant(enumMember));
            i++;
        }

        return expressions.ToArray();
    }
}