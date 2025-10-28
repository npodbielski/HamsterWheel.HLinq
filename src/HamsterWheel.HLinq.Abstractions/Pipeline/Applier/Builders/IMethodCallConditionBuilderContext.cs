using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface IMethodCallConditionBuilderContext : IBuilderContext
{
    MemberExpression? MethodSource { get; set; }
}