using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IParametersConverter
{
    /// <summary>
    /// Attempts conversion of arguments to method call parameter expressions
    /// </summary>
    /// <returns>
    /// The expressions of the call, one per parameter of <paramref name="parameterTypes"/> in their order,
    /// and whether the arguments of the query fill them: when they do not, no expressions come back, for
    /// <c>Expression.Call</c> binds a call of all the parameters of the method or none.
    /// </returns>
    (bool converted, Expression[] expressions) TryConvert(
        (string value, MemberExpression? expression)[] hLinqParams, IBindingParameterInfo[] parameterTypes);
}
