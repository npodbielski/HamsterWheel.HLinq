using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public record QueryableContext(IQueryable? Queryable, Type CurrentResultType, int? Count = null)
    : IQueryableContext
{
    public IHLinqWarnings? Warnings { get; init; }

    public GroupingState? Grouping { get; init; }

    public IQueryableContext Next(IQueryable? queryable, Type currentResultType, int? count = null,
        GroupingState? grouping = null) =>
        new QueryableContext(queryable, currentResultType, count)
        {
            Warnings = Warnings,
            Grouping = grouping ?? Grouping
        };
}
