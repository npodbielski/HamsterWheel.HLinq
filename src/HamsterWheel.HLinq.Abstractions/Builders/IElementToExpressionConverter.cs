using System.Linq.Expressions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Builders;

public interface IElementToExpressionConverter
{
    Expression Build(IBuilderContext context, ITreeElement element);
    Type For();
}