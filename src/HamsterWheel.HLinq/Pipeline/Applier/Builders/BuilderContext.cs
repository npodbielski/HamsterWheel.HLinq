using System.Globalization;
using System.Linq.Expressions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed record BuilderContext(ParameterExpression Param, Type Type, string HLinqQuery, IExpressionBuilder Builder)
    : IBuilderContext
{
    public CultureInfo Culture { get; private init; } = CultureInfo.InvariantCulture;

    public IHLinqWarnings? Warnings { get; private init; }

    public IReadOnlyDictionary<string, MemberExpression>? GroupMembers { get; private init; }

    public static BuilderContext From(Type source, ExpressionBuilder builder, string hLinqQuery,
        CultureInfo culture, IHLinqWarnings? warnings = null,
        IReadOnlyDictionary<string, MemberExpression>? groupMembers = null) =>
        new(Expression.Parameter(source), source, hLinqQuery, builder)
        {
            Culture = culture,
            Warnings = warnings,
            GroupMembers = groupMembers
        };

    public Expression ToExpression(ITreeElement element) => Builder.ToExpression(this, element);

    public MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType) =>
        Builder.ToMemberAssignment(this, element, destinationType);

    public IPropInfo ToPropInfo(ITreeElement element) => Builder.GetToMemberBindingConverter(element)
        .GetPropInfo(this, element);
}