using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IElementToExpressionConverter : ISpecializedFor
{
    Expression Build(IBuilderContext context, ITreeElement element);
}