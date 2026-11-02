using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

/// <summary>
/// Builds expression for nested selects.
/// </summary>
public interface INestedSelectBuilder
{
    Type GetResultType(ITreeElement assignment, ITreeElement nested, IBuilderContext context);

    Expression Build(ITreeElement assignment, ITreeElement nested, IBuilderContext context);
}
