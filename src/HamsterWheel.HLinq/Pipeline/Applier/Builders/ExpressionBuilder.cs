using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Selecting;
using static System.Linq.Expressions.Expression;
using static System.StringComparison;
using static HamsterWheel.HLinq.Pipeline.Applier.Builders.BuilderContext;
using static HamsterWheel.HLinq.Reflection.EfDbFunctionsMatcher;
using static HamsterWheel.HLinq.Reflection.MemberRanking;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed class ExpressionBuilder(
    IPropertiesCache props,
    IMethodsCache methods,
    IConverterFactory converterFactory,
    IParametersConverter parametersConverter,
    IEnumerable<IStaticMethodSource> staticMethodSources)
    : IExpressionBuilder
{
    public IParametersConverter GetParametersConverter() => parametersConverter;

    public LambdaExpression GetFilter(Type source, IWhereRoot query, string hLinqQuery,
        CultureInfo? culture = null, IHLinqWarnings? warnings = null)
    {
        var context = From(source, this, hLinqQuery, culture ?? CultureInfo.InvariantCulture, warnings);
        var body = BuildBody(context, query);

        var delegateType = typeof(Func<,>).MakeGenericType(source, typeof(bool));

        return Lambda(delegateType, body, (IEnumerable<ParameterExpression>)[context.Param]);
    }

    public (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeRoot query,
        string hLinqQuery, CultureInfo? culture = null, IHLinqWarnings? warnings = null) =>
        GetProperty(source, (ITreeBranch)query, hLinqQuery, culture, warnings);

    public (LambdaExpression Expression, Type ResultType) GetSelect(Type source, ISelectRoot query,
        string hLinqQuery, CultureInfo? culture = null, IHLinqWarnings? warnings = null,
        IReadOnlyDictionary<string, MemberExpression>? groupMembers = null)
    {
        var context = From(source, this, hLinqQuery, culture ?? CultureInfo.InvariantCulture, warnings,
            groupMembers);
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

        return (Lambda(delegateType, body, context.Param), resultType);
    }

    public (LambdaExpression Expression, Type ResultType) GetProjection(Type source, IProjectionMembers query,
        string hLinqQuery, CultureInfo? culture = null, IHLinqWarnings? warnings = null)
    {
        var context = From(source, this, hLinqQuery, culture ?? CultureInfo.InvariantCulture, warnings);
        var body = BuildBody(context, query);
        var delegateType = typeof(Func<,>).MakeGenericType(source, body.Type);
        return (Lambda(delegateType, body, context.Param), body.Type);
    }

    public Expression ToExpression<T>(IBuilderContext context, T element) where T : ITreeElement =>
        GetToExpressionConverter(element).Build(context, element);

    public MemberAssignment ToMemberAssignment<T>(IBuilderContext context, T element, Type destinationType)
        where T : ITreeElement =>
        GetToMemberBindingConverter(element).Build(context, element, destinationType);

    public MemberResolution<MethodInfo> ResolveStaticMethod(string? name,
        (string value, MemberExpression? expression)[] parameters)
    {
        var staticMethods = staticMethodSources.SelectMany(s => s.Types)
            .Select(methods.AllStatic).SelectMany(m => m).ToArray();

        return RankMethods(declaredOnType: null, name, Named(staticMethods, name), parameters,
            EfFunctionParametersTransformer);
    }

    public MemberResolution<MethodInfo> ResolveMethod(Type propType, string name,
        (string value, MemberExpression? expression)[] parameters) =>
        RankMethods(propType, name, Named(methods.AllInstance(propType), name), parameters);

    private static MethodInfo[] Named(MethodInfo[] candidates, string? name)
    {
        if (name is null)
        {
            return candidates;
        }

        //two sources can name the same type, and then the very same method arrives twice
        var named = new List<MethodInfo>();
        foreach (var candidate in candidates)
        {
            if (candidate.Name.Equals(name, CurrentCultureIgnoreCase) && !named.Contains(candidate))
            {
                named.Add(candidate);
            }
        }

        return [.. named];
    }

    public IPropertyContext GetProperty(Type sourceType, Expression source, string[] path,
        IBuilderContext? context = null)
    {
        var expression = source;
        var currentType = sourceType;
        var start = 0;

        if (path.Length > 0 && context?.GroupMembers is { } groupMembers)
        {
            var own = props.Resolve(sourceType, path[0]).Member;
            if (own is null && groupMembers.TryGetValue(path[0], out var groupMember))
            {
                expression = groupMember;
                currentType = groupMember.Type;
                start = 1;
            }
            else if (own is not null && groupMembers.ContainsKey(path[0]))
            {
                context.Warnings?.Report(new HLinqMemberWarning(
                    HLinqMemberWarningReason.AmbiguousName,
                    own.Name,
                    own.DeclaringType?.ToString() ?? sourceType.ToString(),
                    [$"{own.DeclaringType?.Name}.{own.Name}", $"group.{GroupingState.KeyName}"],
                    context.HLinqQuery));
            }
        }

        foreach (var prop in path.Skip(start))
        {
            var resolution = props.Resolve(currentType, prop);
            resolution.Report(context);
            var targetProp = resolution.Member;
            if (targetProp is null)
            {
                throw new InvalidPropertyPathException(sourceType, path.JoinWithDot(),
                    [.. props.From(currentType).Select(p => p.Name.ToCamelCase())]);
            }

            currentType = targetProp.PropertyType;
            expression = GetProperty(expression, targetProp);
        }

        return new PropertyContext((MemberExpression)expression, currentType);
    }

    public IElementToMemberAssignmentConverter GetToMemberBindingConverter<T>(T element) where T : ITreeElement =>
        converterFactory.GetToMemberAssignmentConverter(element.GetType());


    public (LambdaExpression Expression, Type ResultType) GroupProjection(GroupingState state) =>
        state.InnerKey is null
            ? BuildGrouping(state, state.RowShape,
                RowsOfGroup(state.GroupParam, state.RowType, state.RowSelector))
            : BuildGrouping(state, NestedRowShape(state), NestedRows(state));

    public GroupingState Nest(GroupingState state, LambdaExpression innerKey) =>
        state with { InnerKey = innerKey };

    private static Type NestedRowShape(GroupingState state) =>
        typeof(Group<,>).MakeGenericType(state.InnerKey!.ReturnType, state.RowShape);

    private MethodCallExpression NestedRows(GroupingState state)
    {
        var innerKey = state.InnerKey!;
        var innerItemType = typeof(IGrouping<,>).MakeGenericType(innerKey.ReturnType, state.RowType);
        var innerParam = Parameter(innerItemType, "inner");
        var innerProjected = NestedRowShape(state);
        var innerRows = RowsOfGroup(innerParam, state.RowType, state.RowSelector);
        var innerBody = MemberInit(
            New(innerProjected),
            Bind(innerProjected.GetProperty(GroupingState.KeyName)!, Property(innerParam, GroupingState.KeyName)),
            Bind(innerProjected.GetProperty(GroupingState.RowsName)!, innerRows));
        var innerLambda = Lambda(typeof(Func<,>).MakeGenericType(innerItemType, innerProjected), innerBody,
            innerParam);

        var groupByMethod = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.GroupBy) && m.GetParameters().Length == 2
                                                            && m.GetParameters()[1].ParameterType is
                                                            { IsGenericType: true } keyFunc
                                                            && keyFunc.GenericTypeArguments.Length == 2)
            .MakeGenericMethod(state.RowType, innerKey.ReturnType);

        var selectMethod = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2
                                                             && m.GetParameters()[1].ParameterType is
                                                             { IsGenericType: true } func
                                                             && func.GenericTypeArguments.Length == 2)
            .MakeGenericMethod(innerItemType, innerProjected);

        var toListMethod = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1)
            .MakeGenericMethod(innerProjected);

        var innerGrouping = Call(groupByMethod, state.GroupParam, innerKey);

        return Call(toListMethod, Call(selectMethod, innerGrouping, innerLambda));
    }

    public (LambdaExpression Expression, Type ResultType) GroupRowSelector(GroupingState state,
        ISelectRoot select, string hLinqQuery, CultureInfo culture, IHLinqWarnings? warnings)
    {
        var groupMembers = new Dictionary<string, MemberExpression>(StringComparer.OrdinalIgnoreCase)
        {
            [GroupingState.KeyName] = state.KeyMember
        };

        return GetSelect(state.RowType, select, hLinqQuery, culture, warnings, groupMembers);
    }

    private static MethodCallExpression RowsOfGroup(ParameterExpression groupParam, Type rowType,
        LambdaExpression rowSelector)
    {
        var selectMethod = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2
                                                             && m.GetParameters()[1].ParameterType is
                                                             { IsGenericType: true } func
                                                             && func.GenericTypeArguments.Length == 2)
            .MakeGenericMethod(rowType, rowSelector.ReturnType);

        var toListMethod = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1)
            .MakeGenericMethod(rowSelector.ReturnType);

        return Call(toListMethod,
            Call(selectMethod, groupParam, rowSelector));
    }

    private (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeBranch query,
        string hLinqQuery, CultureInfo? culture, IHLinqWarnings? warnings = null)
    {
        var context = From(source, this, hLinqQuery, culture ?? CultureInfo.InvariantCulture, warnings);
        var body = (MemberExpression)BuildBody(context, query);

        var propType = body.Type;

        var delegateType = typeof(Func<,>).MakeGenericType(source, propType);

        return (Lambda(delegateType, body, (IEnumerable<ParameterExpression>)[context.Param]), propType);
    }

    private IElementToExpressionConverter GetToExpressionConverter<T>(T element) where T : ITreeElement =>
        converterFactory.GetToExpressionConverterFor(element.GetType());

    private Expression BuildBody(IBuilderContext context, ITreeBranch root) => ToExpression(context, root);

    private static MemberExpression GetProperty(Expression source, PropertyInfo prop) =>
        Property(source, prop);

    private static (LambdaExpression Expression, Type ResultType) BuildGrouping(GroupingState state, Type rowShape,
        MethodCallExpression rows)
    {
        var projected = typeof(Group<,>).MakeGenericType(state.KeyType, rowShape);
        var body = MemberInit(
            New(projected),
            Bind(projected.GetProperty(GroupingState.KeyName)!, state.KeyMember),
            Bind(projected.GetProperty(GroupingState.RowsName)!, rows));

        var delegateType = typeof(Func<,>).MakeGenericType(state.GroupingItemType, projected);
        return (Lambda(delegateType, body, state.GroupParam), projected);
    }

    private class ExpectedMemberOrMemberInitExpressionException() : HLinqQueryException(
        $"At this point type of expression should be {nameof(MemberExpression)} or {nameof(MemberInitExpression)}.");
}