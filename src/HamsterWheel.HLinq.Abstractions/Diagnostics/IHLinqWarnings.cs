namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Bag of warnings
/// </summary>
public interface IHLinqWarnings
{
    /// <summary>
    /// What has been reported so far, in the order the query reached it.
    /// </summary>
    IReadOnlyList<HLinqMemberWarning> Reported { get; }

    /// <summary>
    /// Records one warning. Implementations that do something with it as it arrives - write a header,
    /// log a line - do it here, and are expected to have collected it too.
    /// </summary>
    void Report(HLinqMemberWarning warning);
}
