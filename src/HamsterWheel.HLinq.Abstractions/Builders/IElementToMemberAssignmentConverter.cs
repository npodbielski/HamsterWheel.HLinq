using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public interface IElementToMemberAssignmentConverter
{
    IPropInfo GetPropInfo(IBuilderContext context, ITreeElement element);
    MemberAssignment Build(IBuilderContext context, ITreeElement element, Type destinationType);
    Type For();
}