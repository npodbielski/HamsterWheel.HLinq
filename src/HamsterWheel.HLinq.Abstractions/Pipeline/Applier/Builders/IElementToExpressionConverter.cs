using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IElementToExpressionConverter
{
    Expression Build(IBuilderContext context, ITreeElement element);
    Type For();
}