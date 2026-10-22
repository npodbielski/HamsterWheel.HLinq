using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public interface IQueryableContext
{
    IQueryable? Queryable { get; init; }
    Type CurrentResultType { get; init; }
    int? Count { get; init; }

    IHLinqWarnings? Warnings { get; init; }

    IQueryableContext Next(IQueryable? queryable, Type currentResultType, int? count = null,
        GroupingState? grouping = null);

    GroupingState? Grouping { get; init; }
}