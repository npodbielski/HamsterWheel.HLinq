using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Builders;

public interface IParametersConverter
{
    Expression[] AttemptConversion((string value, MemberExpression? expression)[] hLinqParams,
        IBindingParameterInfo[] parameterTypes);
}