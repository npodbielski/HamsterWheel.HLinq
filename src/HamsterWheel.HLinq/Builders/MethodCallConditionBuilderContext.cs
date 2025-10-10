using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public sealed record MethodCallConditionBuilderContext(IBuilderContext BuilderContext) : IMethodCallConditionBuilderContext
{
    public ParameterExpression Param { get; } = BuilderContext.Param;
    public Type Type { get; } = BuilderContext.Type;
    public IExpressionBuilder Builder { get; } = BuilderContext.Builder;
    public MemberExpression? MethodSource { get; set; }
    public string HLinqQuery { get; } = BuilderContext.HLinqQuery;

    public Expression ToExpression(ITreeElement element) => BuilderContext.Builder.ToExpression(this, element);

    public IPropInfo ToPropInfo(ITreeElement element) =>
        BuilderContext.Builder.GetToMemberBindingConverter(element).GetPropInfo(this, element);

    public MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType) =>
        BuilderContext.Builder.ToMemberAssignment(this, element, destinationType);
}