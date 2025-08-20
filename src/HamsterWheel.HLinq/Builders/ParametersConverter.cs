using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Builders;

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
                expressions.Add(Expression.Constant(bindingParameterInfo.ConstantValue, bindingParameterInfo.ConstantType));
                continue;
            }

            var type = bindingParameterInfo.Info.ParameterType;
            var param = hLinqParams[i];
            if (type == typeof(string))
            {
                expressions.Add(param.expression is not null ? param.expression : Expression.Constant(param.value));
                i++;
                continue;
            }

            if (type.IsEnum)
            {
                if (!param.value.Contains('.') && Enum.TryParse(type, param.value, out var enumMember))
                {
                    expressions.Add(Expression.Constant(enumMember));
                    i++;
                    continue;
                }

                var splits = param.value.Split('.');
                if (string.Equals(splits[0], type.Name, StringComparison.CurrentCultureIgnoreCase)
                    && Enum.TryParse(type, splits[1], out var enumMember1))
                {
                    expressions.Add(Expression.Constant(enumMember1));
                    i++;
                }
            }
        }

        return expressions.ToArray();
    }
}