using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IExpressionBuilder
{
    LambdaExpression GetFilter(Type source, IWhereRoot query, string hLinqQuery, CultureInfo? culture = null,
        IHLinqDiagnostics? diagnostics = null);

    (LambdaExpression Expression, Type ResultType) GetSelect(Type source, ISelectRoot query, string hLinqQuery,
        CultureInfo? culture = null, IHLinqDiagnostics? diagnostics = null,
        IReadOnlyDictionary<string, MemberExpression>? groupMembers = null);

    (LambdaExpression Expression, Type ResultType) GetProjection(Type source, IProjectionMembers query,
        string hLinqQuery, CultureInfo? culture = null, IHLinqDiagnostics? diagnostics = null);

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
        string hLinqQuery, CultureInfo culture, IHLinqDiagnostics? diagnostics = null);

    LambdaExpression GroupAggregateSelector(GroupingState state, IMethod method,
        string hLinqQuery, CultureInfo culture, IHLinqDiagnostics? diagnostics = null);

    (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeRoot query, string hLinqQuery,
        CultureInfo? culture = null, IHLinqDiagnostics? diagnostics = null);

    Expression ToExpression<T>(IBuilderContext context, T element) where T : ITreeElement;

    /// <summary>
    /// Turns a math expression written in the query into one expression.
    /// </summary>
    Expression FoldMath(IBuilderContext context, IReadOnlyList<ITreeElement> children);

    /// <summary>
    /// Builds the value a clause reads: one element, a call over a member, or a math run.
    /// </summary>
    Expression BuildValue(IBuilderContext context, IReadOnlyList<ITreeElement> value);

    /// <summary>
    /// Whether a value answers a type of its own, rather than becoming one it is compared to.
    /// </summary>
    bool AnswersWithItsOwnType(IReadOnlyList<ITreeElement> value);

    /// <summary>
    /// Builds the comparison of the two sides in the type C# compares them in.
    /// </summary>
    Expression BuildComparison(IComparisonToken comparison, Expression left, Expression right);

    MemberAssignment ToMemberAssignment<T>(IBuilderContext context, T element, Type destinationType)
        where T : ITreeElement;

    IElementToMemberAssignmentConverter GetToMemberBindingConverter<T>(T element) where T : ITreeElement;

    IParametersConverter GetParametersConverter();

    MemberResolution<MethodInfo> ResolveStaticMethod(string? name,
        (string value, MemberExpression? expression)[] parameters, CultureInfo? culture = null);

    MemberResolution<MethodInfo> ResolveMethod(Type propType, string name,
        (string value, MemberExpression? expression)[] parameters, CultureInfo? culture = null);

    IPropertyContext GetProperty(Type sourceType, Expression source, string[] path,
        IBuilderContext? context = null);
}