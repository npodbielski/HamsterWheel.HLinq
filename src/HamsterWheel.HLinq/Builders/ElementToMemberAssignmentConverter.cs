using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public abstract class ElementToMemberAssignmentConverter<T> : IElementToMemberAssignmentConverter where T : ITreeElement
{
    public MemberAssignment Build(IBuilderContext context, ITreeElement element, Type destinationType) =>
        Build(context, (T)element, destinationType);

    public Type For() => typeof(T);

    public IPropInfo GetPropInfo(IBuilderContext context, ITreeElement element) => GetPropInfo(context, (T)element);
    protected abstract PropInfo GetPropInfo(IBuilderContext context, T element);
    protected abstract MemberAssignment Build(IBuilderContext context, T element, Type destinationType);
}