namespace HamsterWheel.HLinq.Client;

public class HLinqClientQueryBuilderFactory
{
    public UnorderedHLinqClientQueryBuilder<T> For<T>() => new();
}