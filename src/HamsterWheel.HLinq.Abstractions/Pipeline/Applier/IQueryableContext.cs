using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public interface IQueryableContext
{
    IQueryable? Queryable { get; init; }
    Type CurrentResultType { get; init; }
    int? Count { get; init; }

    IHLinqDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Whether HLinq only validates the query: the roots are built into expressions and nothing is
    /// run against the source.
    /// </summary>
    bool ValidateMode { get; init; }

    IQueryableContext Next(IQueryable? queryable, Type currentResultType, int? count = null,
        GroupingState? grouping = null);

    GroupingState? Grouping { get; init; }
}
