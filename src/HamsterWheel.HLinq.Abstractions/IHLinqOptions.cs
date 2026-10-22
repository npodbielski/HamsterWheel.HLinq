namespace HamsterWheel.HLinq;

public interface IHLinqOptions
{
    int HttpDefaultMaxTakeRecords { get; }

    /// <summary>
    /// If warnings should be set in response or not.
    /// </summary>
    bool EmitWarnings { get; }
}