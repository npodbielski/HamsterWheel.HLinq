using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public class StaticMethodToExpressionConverter : IStaticMethodToExpressionConverter
{
    public Expression BuildStatic(IBuilderContext context, IMethod method, IParametersConverter parametersConverter)
    {
        var name = method.GetName(context.HLinqQuery);
        var parameters = method.Children.OfType<IMethodParamElement>()
            .Select(p => (p.GetValue(context.HLinqQuery),
                p is Property property ? context.ToExpression(property) as MemberExpression : null))
            .ToArray();
        var methods = context.Builder.GetMostProbableMethods(name, parameters);

        MethodCallExpression? methodCallExpression = null;
        foreach (var info in methods)
        {
            var expressions =
                parametersConverter.AttemptConversion(parameters, info.parameters.ToArray());
            try
            {
                methodCallExpression = Expression.Call(null, info.method, expressions);
            }
            catch (ArgumentException)
            {
                //swallow exception and try to bind another method from list of available
            }
        }

        return methodCallExpression ??
               throw new Method.InvalidStaticMethodException(name, methods.Select(m => m.method.Name).ToArray());
    }
}