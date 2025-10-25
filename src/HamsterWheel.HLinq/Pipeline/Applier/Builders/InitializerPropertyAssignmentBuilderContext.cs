using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed record InitializerPropertyAssignmentBuilderContext(IBuilderContext BuilderContext) : IInitializerPropertyAssignmentBuilderContext
{
    public Type? InitializerPropertyType { get; set; }
    public ParameterExpression Param { get; } = BuilderContext.Param;
    public Type Type { get; } = BuilderContext.Type;
    public IExpressionBuilder Builder { get; } = BuilderContext.Builder;
    public string HLinqQuery { get; } = BuilderContext.HLinqQuery;

    public Expression ToExpression(ITreeElement element) => BuilderContext.Builder.ToExpression(this, element);

    public IPropInfo ToPropInfo(ITreeElement element) =>
        BuilderContext.Builder.GetToMemberBindingConverter(element).GetPropInfo(this, element);

    public MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType) =>
        BuilderContext.Builder.ToMemberAssignment(this, element, destinationType);
}