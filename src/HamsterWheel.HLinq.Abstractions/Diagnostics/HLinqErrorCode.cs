namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Code of hlinq query error.
/// </summary>
public enum HLinqErrorCode
{
    /// <summary>
    /// Query asked for not existing member. Examples contains list of available members.
    /// </summary>
    UnknownMember,

    /// <summary>
    /// String value of constant provided in a query cannot be converted to destination type.
    /// </summary>
    ImpossibleConversion,

    /// <summary>
    /// Two values in comparison cannot be compared.
    /// </summary>
    InvalidComparison,

    /// <summary>
    /// Two constants in a comparison, which is an invalid query.
    /// </summary>
    ConstantOnlyComparison,

    /// <summary>
    /// Invalid condition was provided.
    /// </summary>
    NotACondition,

    /// <summary>
    /// Set of method parameters provided in a query does not match any overload. Examples
    /// contains its signatures.
    /// </summary>
    InvalidMethodParameters,

    /// <summary>
    /// Provided name is not an aggregate function name. Examples contains list of available
    /// aggregates.
    /// </summary>
    NotAnAggregate,

    /// <summary>
    /// Aggregate method used in incorrect context, where nothing is grouped, so it has nothing to
    /// fold.
    /// </summary>
    AggregateNeedsGroup,

    /// <summary>
    /// The value provided in a paging root is invalid.
    /// </summary>
    InvalidPagingValue,

    /// <summary>
    /// Two properties of the query were named the same.
    /// </summary>
    DuplicateMemberName,

    /// <summary>
    /// Query used root token in an incorrect context.
    /// </summary>
    RootNotAllowed,

    /// <summary>
    /// Provided query was invalid and could not be parsed. Text is the slice that stopped the
    /// parse.
    /// </summary>
    InvalidQuery,

    /// <summary>
    /// The query is longer than MaximumQueryLength.
    /// </summary>
    QueryTooLong,

    /// <summary>
    /// More properties than MaximumNumberOfProperties.
    /// </summary>
    TooManyProperties,

    /// <summary>
    /// Culture specified in the query was not recognized.
    /// </summary>
    UnknownCulture,

    /// <summary>
    /// General case for invalid query. The message is the whole answer.
    /// </summary>
    Refused,
}
