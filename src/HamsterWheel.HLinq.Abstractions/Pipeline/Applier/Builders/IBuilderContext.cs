using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IBuilderContext
{
    ParameterExpression Param { get; }
    Type Type { get; }
    IExpressionBuilder Builder { get; }
    string HLinqQuery { get; }
    Expression ToExpression(ITreeElement element);
    IPropInfo ToPropInfo(ITreeElement element);
    MemberAssignment ToMemberAssignment(ITreeElement element, Type destinationType);
}