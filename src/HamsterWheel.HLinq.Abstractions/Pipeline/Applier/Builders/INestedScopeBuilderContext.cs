using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public interface INestedScopeBuilderContext : IBuilderContext
{
    Expression InnerSource { get; }
    Type InnerType { get; }
}
