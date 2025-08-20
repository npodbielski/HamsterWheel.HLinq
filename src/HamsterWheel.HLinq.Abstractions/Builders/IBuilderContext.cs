using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public interface IBuilderContext
{
    ParameterExpression Param { get; }
    Type Type { get; }
    IExpressionBuilder Builder { get; }
    MemberExpression? MethodSource { get; set; }
    Type? ComparisonPropertyType { get; set; }
    string HLinqQuery { get; }
    Type? InitializerPropertyType { get; set; }
    Expression ToExpression(ITreeElement element);
    IPropInfo ToPropInfo(ITreeElement element);
    MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType);
}