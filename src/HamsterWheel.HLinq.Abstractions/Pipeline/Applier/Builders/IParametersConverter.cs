using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IParametersConverter
{
    Expression[] AttemptConversion((string value, MemberExpression? expression)[] hLinqParams,
        IBindingParameterInfo[] parameterTypes);
}