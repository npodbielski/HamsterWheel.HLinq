using System.Globalization;
using System.Linq.Expressions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IBuilderContext
{
    ParameterExpression Param { get; }
    Type Type { get; }
    IExpressionBuilder Builder { get; }
    string HLinqQuery { get; }
    CultureInfo Culture { get; }
    IHLinqWarnings? Warnings { get; }

    /// <summary>
    /// Group keys. Null otherwise.
    /// </summary>
    IReadOnlyDictionary<string, MemberExpression>? GroupMembers { get; }
    Expression ToExpression(ITreeElement element);
    IPropInfo ToPropInfo(ITreeElement element);
    MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType);
}