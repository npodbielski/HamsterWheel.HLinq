namespace HamsterWheel.HLinq;

public class HLinqOptions : IHLinqOptions
{
    /// <summary>
    /// Default value for default maximum number of records when applied to <see cref="IQueryable"/> inside the HTTP pipeline when no <code>take[number]</code> part of query was specified.
    /// </summary>
    public const int DefaultMaxTakeRecords = 1000;

    /// <summary>
    /// Default maximum number of records when applied to <see cref="IQueryable"/> inside the HTTP pipeline when no <code>take[number]</code> part of query was specified.
    /// </summary>
    public int HttpDefaultMaxTakeRecords { get; init; } = DefaultMaxTakeRecords;

    /// <summary>
    /// Whether a warning about a member the query could not decide between goes out to the caller on the
    /// <c>X-HLinq-Warning</c> header of the response. The log carries warnings whether this is on or not.
    /// </summary>
    public bool EmitWarnings { get; init; } = true;
}