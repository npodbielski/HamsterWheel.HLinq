namespace HamsterWheel.HLinq;

public interface IHLinqOptions
{
    int HttpDefaultMaxTakeRecords { get; }

    /// <summary>
    /// If warnings should be set in response or not.
    /// </summary>
    bool EmitWarnings { get; }

    /// <summary>
    /// Maximum allowed length of hlinq query, in characters.
    /// </summary>
    int MaximumQueryLength { get; }

    /// <summary>
    /// Maximum number of properties allowed in a single query.
    /// </summary>
    int MaximumNumberOfProperties { get; }
}
