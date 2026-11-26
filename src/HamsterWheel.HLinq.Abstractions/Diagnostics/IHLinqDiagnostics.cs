using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Collection of hlinq query errors and warnings.
/// </summary>
public interface IHLinqDiagnostics
{
    IReadOnlyList<HLinqError> Errors { get; }

    IReadOnlyList<HLinqMemberWarning> Warnings { get; }

    /// <summary>
    /// Set if query was invalid and could not be processed.
    /// </summary>
    HLinqQueryException? Exception { get; }

    /// <summary>
    /// The English keyword of the root being built, null when no root is.
    /// </summary>
    string? RootKeyword { get; set; }

    /// <summary>
    /// Records an error.
    /// </summary>
    void Report(HLinqError error);

    /// <summary>
    /// Records one warning.
    /// </summary>
    void Report(HLinqMemberWarning warning);

    /// <summary>
    /// Records an exception as an error.
    /// </summary>
    void AddException(HLinqQueryException exception);
}
