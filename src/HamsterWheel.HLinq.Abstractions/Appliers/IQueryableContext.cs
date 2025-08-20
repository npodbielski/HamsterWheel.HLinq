namespace HamsterWheel.HLinq.Appliers;

public interface IQueryableContext
{
    IQueryable? Queryable { get; init; }
    Type CurrentResultType { get; init; }
    int? Count { get; init; }
}