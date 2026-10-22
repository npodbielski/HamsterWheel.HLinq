using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IExpressionBuilder
{
    LambdaExpression GetFilter(Type source, IWhereRoot query, string hLinqQuery, CultureInfo? culture = null,
        IHLinqWarnings? warnings = null);

    (LambdaExpression Expression, Type ResultType) GetSelect(Type source, ISelectRoot query, string hLinqQuery,
        CultureInfo? culture = null, IHLinqWarnings? warnings = null,
        IReadOnlyDictionary<string, MemberExpression>? groupMembers = null);

    (LambdaExpression Expression, Type ResultType) GetProjection(Type source, IProjectionMembers query,
        string hLinqQuery, CultureInfo? culture = null, IHLinqWarnings? warnings = null);

    /// <summary>
    /// Creates Expression for GroupBy projection.
    /// </summary>
    (LambdaExpression Expression, Type ResultType) GroupProjection(GroupingState state);

    /// <summary>
    /// Adds inner key to grouping of a group written after a grouping.
    /// </summary>
    GroupingState Nest(GroupingState state, LambdaExpression innerKey);

    /// <summary>
    /// Creates Expression for select of rows inside a group.
    /// </summary>
    (LambdaExpression Expression, Type ResultType) GroupRowSelector(GroupingState state, ISelectRoot select,
        string hLinqQuery, CultureInfo culture, IHLinqWarnings? warnings);

    (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeRoot query, string hLinqQuery,
        CultureInfo? culture = null, IHLinqWarnings? warnings = null);

    Expression ToExpression<T>(IBuilderContext context, T element) where T : ITreeElement;

    MemberAssignment ToMemberAssignment<T>(IBuilderContext context, T element, Type destinationType)
        where T : ITreeElement;

    IElementToMemberAssignmentConverter GetToMemberBindingConverter<T>(T element) where T : ITreeElement;

    IParametersConverter GetParametersConverter();

    MemberResolution<MethodInfo> ResolveStaticMethod(string? name,
        (string value, MemberExpression? expression)[] parameters);

    MemberResolution<MethodInfo> ResolveMethod(Type propType, string name,
        (string value, MemberExpression? expression)[] parameters);

    IPropertyContext GetProperty(Type sourceType, Expression source, string[] path,
        IBuilderContext? context = null);
}