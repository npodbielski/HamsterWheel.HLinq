using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Contains hlinq errors and warnings.
/// </summary>
public class HLinqDiagnostics : IHLinqDiagnostics
{
    private readonly List<HLinqError> _errors = [];
    private readonly List<HLinqMemberWarning> _warnings = [];

    public IReadOnlyList<HLinqError> Errors => _errors;

    public IReadOnlyList<HLinqMemberWarning> Warnings => _warnings;

    public HLinqQueryException? Exception { get; private set; }

    public string? RootKeyword { get; set; }

    public void Report(HLinqError error) => _errors.Add(error);

    public virtual void Report(HLinqMemberWarning warning) => _warnings.Add(warning);

    public void AddException(HLinqQueryException exception)
    {
        if (!exception.ShouldReportToClient)
        {
            return;
        }

        Report(HLinqError.Create(exception.Code, RootKeyword, exception.InvalidQueryChunk, exception.Message,
            exception.Examples));

        Exception ??= exception;
    }
}
