namespace HamsterWheel.HLinq.Builders;

public class StaticMethodSourceWrapper(IEnumerable<IStaticMethodSource> childSource) : IStaticMethodSource
{
    public Type[] Types { get; } = childSource.SelectMany(c => c.Types).ToArray();
}