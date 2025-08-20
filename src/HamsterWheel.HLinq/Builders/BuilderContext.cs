using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public sealed record BuilderContext(ParameterExpression Param, Type Type, string HLinqQuery, IExpressionBuilder Builder)
    : IBuilderContext
{
    public MemberExpression? MethodSource { get; set; }
    public Type? ComparisonPropertyType { get; set; }
    public Type? InitializerPropertyType { get; set; }

    public static BuilderContext From(Type source, ExpressionBuilder builder, string hLinqQuery) =>
        new(Expression.Parameter(source), source, hLinqQuery, builder);

    public Expression ToExpression(ITreeElement element) => Builder.ToExpression(this, element);

    public MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType) =>
        Builder.ToMemberAssignment(this, element, destinationType);

    public IPropInfo ToPropInfo(ITreeElement element) => Builder.GetToMemberBindingConverter(element)
        .GetPropInfo(this, element);
}

public record PropInfo(string Name, Type Type) : IPropInfo;