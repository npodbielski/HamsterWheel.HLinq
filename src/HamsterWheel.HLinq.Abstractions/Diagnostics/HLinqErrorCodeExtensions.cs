namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Translates HLinqErrorCode enum to string
/// </summary>
public static class HLinqErrorCodeExtensions
{
    /// <summary>
    /// The error code a client reads, i.e. "unknown-member".
    /// </summary>
    public static string ToErrorString(this HLinqErrorCode code) => code switch
    {
        HLinqErrorCode.UnknownMember => "unknown-member",
        HLinqErrorCode.ImpossibleConversion => "impossible-conversion",
        HLinqErrorCode.InvalidComparison => "invalid-comparison",
        HLinqErrorCode.ConstantOnlyComparison => "constant-only-comparison",
        HLinqErrorCode.NotACondition => "not-a-condition",
        HLinqErrorCode.InvalidMethodParameters => "invalid-method-parameters",
        HLinqErrorCode.NotAnAggregate => "not-an-aggregate",
        HLinqErrorCode.AggregateNeedsGroup => "aggregate-needs-group",
        HLinqErrorCode.InvalidPagingValue => "invalid-paging-value",
        HLinqErrorCode.DuplicateMemberName => "duplicate-member-name",
        HLinqErrorCode.RootNotAllowed => "root-not-allowed",
        HLinqErrorCode.InvalidQuery => "invalid-query",
        HLinqErrorCode.QueryTooLong => "query-too-long",
        HLinqErrorCode.TooManyProperties => "too-many-properties",
        HLinqErrorCode.UnknownCulture => "unknown-culture",
        HLinqErrorCode.Refused => "refused",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };
}
