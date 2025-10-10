using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public class PropertyMethodToExpressionConverter : IPropertyMethodToExpressionConverter
{
    public Expression BuildInstance(IBuilderContext context, IMethod method,
        IParametersConverter parametersConverter)
    {
        var expression = context.MethodSource;
        var propType = context.MethodSource!.Type;
        var name = method.GetName(context.HLinqQuery);
        var parameters = method.Children.OfType<IMethodParamElement>()
            .Select(p => (p.GetValue(context.HLinqQuery),
                p is Property property ? context.ToExpression(property) as MemberExpression : null))
            .ToArray();
        var methods = context.Builder.GetMostProbableMethods(propType, name, parameters);

        MethodCallExpression? methodCallExpression = null;
        foreach (var info in methods)
        {
            var expressions = parametersConverter.AttemptConversion(parameters,
                info.GetParameters()
                    .Select<ParameterInfo, IBindingParameterInfo>(m => new BindingParameterInfo
                        { Info = m }).ToArray());
            try
            {
                methodCallExpression = Expression.Call(expression, info, expressions);
            }
            catch (ArgumentException)
            {
                //swallow exception and try to bind another method from list of available
            }
        }

        return methodCallExpression ??
               throw new Method.Converter.InvalidMethodException(propType, context.MethodSource.Member.Name, name,
                   methods.Select(m => m.Name).ToArray());
    }
}