using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Builders;

public interface IMethodCallConditionBuilderContext : IBuilderContext
{
    MemberExpression? MethodSource { get; set; }
}