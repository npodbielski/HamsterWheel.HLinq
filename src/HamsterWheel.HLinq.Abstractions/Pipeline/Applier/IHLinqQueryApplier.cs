using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public interface IHLinqQueryApplier
{
    object Apply<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery, CancellationToken token = default);

    object Apply(IQueryable queryable, Type itemType, IHLinqQuery hLinqQuery, CancellationToken token = default);

    IResult ApplyGetType<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery, CancellationToken token = default);

    IResult ApplyGetType(IQueryable queryable, IHLinqQuery hLinqQuery, Type itemType, CancellationToken token = default);

    /// <summary>
    /// Validates hlinq query without running it against real data.
    /// </summary>
    /// <param name="hLinqQuery">The query, already parsed.</param>
    /// <param name="itemType">The type of a row the query would be applied to.</param>
    /// <param name="diagnostics">Where the errors are collected.</param>
    /// <returns>List of errors</returns>
    IReadOnlyList<HLinqError> Validate(IHLinqQuery hLinqQuery, Type itemType, IHLinqDiagnostics diagnostics);
}
