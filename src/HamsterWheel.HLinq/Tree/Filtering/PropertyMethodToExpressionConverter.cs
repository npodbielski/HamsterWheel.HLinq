using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public class PropertyMethodToExpressionConverter : IPropertyMethodToExpressionConverter
{
    public Expression BuildInstance(IBuilderContext context, IMethod method,
        IParametersConverter parametersConverter)
    {
        var methodCallContext = context as IMethodCallConditionBuilderContext ??
                                throw new PropertyMethodCallNeedsToHaveSourceExpressionException();
        var source = methodCallContext.MethodSource ??
                     throw new PropertyMethodCallNeedsToHaveSourceExpressionException();
        var propType = source.Type;
        var name = method.GetName(context.HLinqQuery);
        var parameters = method.Children.OfType<IMethodParamElement>()
            .Select(p => (value: p.GetValue(context.HLinqQuery),
                expression: p is Property property ? context.ToExpression(property) as MemberExpression : null))
            .ToArray();
        var resolution = context.Builder.ResolveMethod(propType, name, parameters);
        resolution.Report(context);

        foreach (var candidate in resolution.Competitors)
        {
            var (converted, expressions) = parametersConverter.TryConvert(parameters,
                [
                    .. candidate.GetParameters()
                        .Select<ParameterInfo, IBindingParameterInfo>(m => new BindingParameterInfo
                            { Info = m })
                ]);
            if (!converted)
            {
                continue;
            }

            try
            {
                return Expression.Call(source, candidate, expressions);
            }
            catch (ArgumentException)
            {
                //Expression.Call can still refuse to bind the candidate the ranking put first, and then
                //the one below it is tried
            }
        }

        throw new Method.InvalidMethodException(propType, source.Member.Name, name,
            [.. parameters.Select(p => p.value)], [.. resolution.Competitors]);
    }

    private sealed class PropertyMethodCallNeedsToHaveSourceExpressionException()
        : HLinqQueryException("At this point method source needs to have value!");
}