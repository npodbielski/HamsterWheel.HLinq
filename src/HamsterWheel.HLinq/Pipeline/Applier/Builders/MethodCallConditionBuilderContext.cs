using System.Globalization;
using System.Linq.Expressions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed record MethodCallConditionBuilderContext(IBuilderContext BuilderContext)
    : IMethodCallConditionBuilderContext
{
    public ParameterExpression Param { get; } = BuilderContext.Param;
    public Type Type { get; } = BuilderContext.Type;
    public IExpressionBuilder Builder { get; } = BuilderContext.Builder;
    public MemberExpression? MethodSource { get; set; }
    public string HLinqQuery { get; } = BuilderContext.HLinqQuery;
    public CultureInfo Culture { get; } = BuilderContext.Culture;
    public IHLinqWarnings? Warnings { get; } = BuilderContext.Warnings;
    public IReadOnlyDictionary<string, MemberExpression>? GroupMembers { get; } = BuilderContext.GroupMembers;

    public Expression ToExpression(ITreeElement element) => BuilderContext.Builder.ToExpression(this, element);

    public IPropInfo ToPropInfo(ITreeElement element) =>
        BuilderContext.Builder.GetToMemberBindingConverter(element).GetPropInfo(this, element);

    public MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType) =>
        BuilderContext.Builder.ToMemberAssignment(this, element, destinationType);
}