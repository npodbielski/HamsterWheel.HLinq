namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Collection of HLinqWarnings that may impact the correctness of the query but are not strictly an error
/// </summary>
public class HLinqWarnings : IHLinqWarnings
{
    private readonly List<HLinqMemberWarning> _warnings = [];

    public IReadOnlyList<HLinqMemberWarning> Reported => _warnings;

    public virtual void Report(HLinqMemberWarning warning) => _warnings.Add(warning);
}
