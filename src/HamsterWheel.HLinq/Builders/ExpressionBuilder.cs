using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.Common.Reflection;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filter;
using HamsterWheel.HLinq.Tree.Select;

namespace HamsterWheel.HLinq.Builders;

public sealed class ExpressionBuilder(
    IPropertiesCache props,
    IMethodsCache methods,
    IConverterFactory converterFactory,
    IParametersConverter parametersConverter,
    IEnumerable<IStaticMethodSource> staticMethodSources)
    : IExpressionBuilder
{
    public LambdaExpression GetFilter(Type source, IWhereRoot query, string hLinqQuery)
    {
        var context = BuilderContext.From(source, this, hLinqQuery);
        var body = BuildBody(context, query);

        var delegateType = typeof(Func<,>).MakeGenericType(source, typeof(bool));

        return Expression.Lambda(delegateType, body, (IEnumerable<ParameterExpression>) [context.Param]);
    }

    public (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeRoot query, string hLinqQuery) =>
        GetProperty(source, (ITreeBranch)query, hLinqQuery);

    public (LambdaExpression Expression, Type ResultType) GetSelect(Type source, ISelectRoot query, string hLinqQuery)
    {
        var context = BuilderContext.From(source, this, hLinqQuery);
        var expression = BuildBody(context, query);
        Expression body;
        Type resultType;
        if (expression is MemberInitExpression memberInitExpression)
        {
            body = memberInitExpression;
            resultType = body.Type;
        }
        else
        {
            var propertyExpression = expression as MemberExpression ??
                                     throw new ExpectedMemberOrMemberInitExpressionException();
            body = expression;
            resultType = propertyExpression.Type;
        }

        var delegateType = typeof(Func<,>).MakeGenericType(source, resultType);

        return (Expression.Lambda(delegateType, body, (IEnumerable<ParameterExpression>) [context.Param]), resultType);
    }

    public Expression BuildBody(IBuilderContext context, ITreeBranch root) => ToExpression(context, root);

    public Expression ToExpression<T>(IBuilderContext context, T element) where T : ITreeElement =>
        GetToExpressionConverter(element).Build(context, element);

    public MemberAssignment ToMemberAssignment<T>(IBuilderContext context, T element, Type destinationType)
        where T : ITreeElement =>
        GetToMemberBindingConverter(element).Build(context, element, destinationType);

    public (MethodInfo method, IBindingParameterInfo[] parameters)[] GetMostProbableMethods(string? name,
        (string value, MemberExpression? expression)[] parameters)
    {
        var staticMethods = staticMethodSources.SelectMany(s => s.Types)
            .Select(methods.AllStatic).SelectMany(m => m).ToArray();

        IEnumerable<MethodInfo> allMethods = staticMethods;
        if (name is not null)
        {
            allMethods = staticMethods.Where(m => m.Name == name)
                .Concat(staticMethods.Where(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
                .Distinct();
        }

        return FilterMethodsViaParameterValues(parameters, allMethods, ParametersTransformer)
            .Select(m => (m, BindingParametersTransformer(m.GetParameters()).ToArray()))
            .ToArray();

        ParameterInfo[] ParametersTransformer(ParameterInfo[] infos)
        {
            //TODO: this should be part of IStaticMethodSource
            if (infos[0].Name == "_" && infos[0].ParameterType.Name == "DbFunctions")
            {
                return infos[1..];
            }

            return infos;
        }

        IBindingParameterInfo[] BindingParametersTransformer(ParameterInfo[] infos)
        {
            var list = new List<BindingParameterInfo>();
            //TODO: this should be part of IStaticMethodSource
            foreach (var info in infos)
            {
                if (info is { Name: "_", ParameterType.Name: "DbFunctions" })
                {
                    list.Add(new BindingParameterInfo
                    {
                        Info = info,
                        ConstantValue = null,
                        ConstantType = info.ParameterType
                    });
                }
                else
                {
                    list.Add(new BindingParameterInfo { Info = info });
                }
            }

            return list.ToArray();
        }
    }

    public MethodInfo[] GetMostProbableMethods(Type propType, string name,
        (string value, MemberExpression? expression)[] parameters)
    {
        var allInstance = methods.AllInstance(propType);
        var methodInfos = allInstance.Where(m => m.Name == name)
            .Concat(allInstance.Where(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))).Distinct();

        return FilterMethodsViaParameterValues(parameters, methodInfos);
    }

    private static MethodInfo[] FilterMethodsViaParameterValues(
        (string value, MemberExpression? expression)[] parameters,
        IEnumerable<MethodInfo> methodInfos, Func<ParameterInfo[], ParameterInfo[]>? parametersTransformer = null)
    {
        var methodsWithProbability = new List<(int probability, MethodInfo method)>();
        foreach (var method in methodInfos)
        {
            var parameterIndex = 0;
            var probability = 100;
            var parameterInfos = parametersTransformer is not null
                ? parametersTransformer(method.GetParameters())
                : method.GetParameters();
            if (parameterInfos.Length != parameters.Length)
            {
                continue;
            }

            foreach (var parameterInfo in parameterInfos)
            {
                var hlinqParameter = parameters[parameterIndex];
                if (parameterInfo.ParameterType == typeof(char) && (hlinqParameter.value.Length == 1 ||
                                                                    hlinqParameter.expression?.Member.ReflectedType ==
                                                                    typeof(char)))
                {
                    probability = 0;
                    break;
                }

                if (parameterInfo.ParameterType.IsEnum)
                {
                    var splits = hlinqParameter.value.Contains('.')
                        ? hlinqParameter.value.Split('.')
                        : [hlinqParameter.value];
                    if (splits[0] == parameterInfo.ParameterType.Name &&
                        Enum.GetNames(parameterInfo.ParameterType).Contains(splits[0]))
                    {
                        probability += 100 / parameters.Length;
                    }
                }

                parameterIndex++;
            }

            if (probability > 0)
            {
                methodsWithProbability.Add((probability, method));
            }
        }

        return methodsWithProbability.OrderBy(p => p.probability).Select(p => p.method).ToArray();
    }

    public IPropertyContext GetPropertyWithType(Type sourceType, Expression source, string[] path)
    {
        var expression = source;
        var currentType = sourceType;
        foreach (var prop in path)
        {
            var targetProp = props.Single(currentType, prop);
            if (targetProp is null)
            {
                var join = string.Join('.', path);
                throw new InvalidPropertyPathException(sourceType, join,
                    props.From(currentType).Select(p => p.Name.ToCamelCase()).ToArray());
            }

            currentType = targetProp.PropertyType;
            //TODO: it would be nice to attempt to create expression if all types are just objects but I am not sure if this is even possible via expressions
            //...to make this work expression builder needs to have an access to actual object here which is not possible currently since this whole code does not know about the actual data just types
            // if (currentType == typeof(object) && targetProp.GetMethod is not null)
            // {
            //     currentType = targetProp.GetMethod.Invoke();
            // }
            expression = GetProperty(expression, targetProp);
        }

        return new PropertyContext((MemberExpression)expression, currentType);
    }

    public IElementToMemberAssignmentConverter GetToMemberBindingConverter<T>(T element) where T : ITreeElement =>
        converterFactory.GetToMemberAssignmentConverter(element.GetType());

    private (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeBranch query, string hLinqQuery)
    {
        var context = BuilderContext.From(source, this, hLinqQuery);
        var body = (MemberExpression)BuildBody(context, query);

        var propType = body.Type;

        var delegateType = typeof(Func<,>).MakeGenericType(source, propType);

        return (Expression.Lambda(delegateType, body, (IEnumerable<ParameterExpression>) [context.Param]), propType);
    }

    private IElementToExpressionConverter GetToExpressionConverter<T>(T element) where T : ITreeElement =>
        converterFactory.GetToExpressionConverterFor(element.GetType());

    private static MemberExpression GetProperty(Expression source, PropertyInfo prop) =>
        Expression.Property(source, prop);

    public IParametersConverter GetParametersConverter() => parametersConverter;
}