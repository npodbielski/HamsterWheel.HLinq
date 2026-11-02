using System.Globalization;
using System.Linq.Expressions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

/// <summary>
/// HLinq nested selection builder context.
/// </summary>
public sealed record NestedScopeBuilderContext(IBuilderContext Outer, Expression InnerSource, Type InnerType)
    : INestedScopeBuilderContext
{
    public ParameterExpression Param => Outer.Param;
    public Type Type => Outer.Type;
    public string HLinqQuery => Outer.HLinqQuery;
    public IExpressionBuilder Builder => Outer.Builder;
    public CultureInfo Culture => Outer.Culture;
    public IHLinqWarnings? Warnings => Outer.Warnings;
    public IReadOnlyDictionary<string, MemberExpression>? GroupMembers => Outer.GroupMembers;

    public Expression ToExpression(ITreeElement element) => Builder.ToExpression(this, element);

    public IPropInfo ToPropInfo(ITreeElement element) =>
        Builder.GetToMemberBindingConverter(element).GetPropInfo(this, element);

    public MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType) =>
        Builder.ToMemberAssignment(this, element, destinationType);
}
