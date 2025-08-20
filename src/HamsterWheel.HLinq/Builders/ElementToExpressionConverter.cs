using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public abstract class ElementToExpressionConverter<T> : IElementToExpressionConverter where T : ITreeElement
{
    public Expression Build(IBuilderContext context, ITreeElement element) => Build(context, (T)element);

    public Type For() => typeof(T);

    protected abstract Expression Build(IBuilderContext context, T element);
}