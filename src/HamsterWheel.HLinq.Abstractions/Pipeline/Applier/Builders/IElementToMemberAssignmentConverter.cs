using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IElementToMemberAssignmentConverter
{
    IPropInfo GetPropInfo(IBuilderContext context, ITreeElement element);
    MemberAssignment Build(IBuilderContext context, ITreeElement element, Type destinationType);
    Type For();
}