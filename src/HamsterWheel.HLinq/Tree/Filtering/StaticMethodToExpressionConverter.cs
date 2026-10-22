using System.Linq.Expressions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public class StaticMethodToExpressionConverter : IStaticMethodToExpressionConverter
{
    public Expression BuildStatic(IBuilderContext context, IMethod method, IParametersConverter parametersConverter)
    {
        var name = method.GetName(context.HLinqQuery);
        var parameters = method.Children.OfType<IMethodParamElement>()
            .Select(p => (value: p.GetValue(context.HLinqQuery),
                expression: p is Property property ? context.ToExpression(property) as MemberExpression : null))
            .ToArray();
        var resolution = context.Builder.ResolveStaticMethod(name, parameters);
        resolution.Report(context);

        foreach (var candidate in resolution.Competitors)
        {
            var (converted, expressions) = parametersConverter.TryConvert(parameters,
                StaticMethodSourceWrapper.MapEfDbFunctionParams(candidate.GetParameters()));
            if (!converted)
            {
                continue;
            }

            try
            {
                return Expression.Call(null, candidate, expressions);
            }
            catch (ArgumentException)
            {
                //Expression.Call can still refuse to bind the candidate the ranking put first, and then
                //the one below it is tried
            }
        }

        throw new Method.InvalidStaticMethodException(name, [.. parameters.Select(p => p.value)],
            [.. resolution.Competitors]);
    }
}
