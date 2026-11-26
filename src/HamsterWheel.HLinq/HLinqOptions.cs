namespace HamsterWheel.HLinq;

public class HLinqOptions : IHLinqOptions
{
    /// <summary>
    /// Default value for default maximum number of records when applied to <see cref="IQueryable"/> inside the HTTP pipeline when no <code>take[number]</code> part of query was specified.
    /// </summary>
    public const int DefaultMaxTakeRecords = 1000;

    /// <summary>
    /// Default for <see cref="MaximumQueryLength"/>.
    /// </summary>
    public const int DefaultMaximumQueryLength = 4096;

    /// <summary>
    /// Default for <see cref="MaximumNumberOfProperties"/>.
    /// </summary>
    public const int DefaultMaximumNumberOfProperties = 100;

    /// <summary>
    /// Default maximum number of records when applied to <see cref="IQueryable"/> inside the HTTP pipeline when no <code>take[number]</code> part of query was specified.
    /// </summary>
    public int HttpDefaultMaxTakeRecords { get; init; } = DefaultMaxTakeRecords;

    /// <summary>
    /// Whether a warning about a member the query could not decide between goes out to the caller on the
    /// <c>X-HLinq-Warning</c> header of the response. The log carries warnings whether this is on or not.
    /// </summary>
    public bool EmitWarnings { get; init; } = true;

    /// <summary>
    /// Maximum allowed length of hlinq query, in characters.
    /// </summary>
    public int MaximumQueryLength { get; init; } = DefaultMaximumQueryLength;

    /// <summary>
    /// Maximum number of properties allowed in a single query.
    /// </summary>
    public int MaximumNumberOfProperties { get; init; } = DefaultMaximumNumberOfProperties;
}
