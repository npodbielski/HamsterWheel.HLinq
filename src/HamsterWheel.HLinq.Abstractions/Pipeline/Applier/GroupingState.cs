using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier;

/// <summary>
/// Group state data
/// </summary>
public sealed record GroupingState(
    Type RowType,
    Type KeyType,
    Type GroupingItemType,
    ParameterExpression GroupParam,
    MemberExpression KeyMember,
    LambdaExpression RowSelector,
    Type RowShape,
    LambdaExpression? InnerKey = null)
{
    /// <summary>
    /// The name a grouped row context reads the key of its group under.
    /// </summary>
    public const string KeyName = "Key";

    /// <summary>
    /// Name of property that holds group rows
    /// </summary>
    public const string RowsName = "Rows";

    /// <summary>
    /// The names a grouped answer holds before the query writes any member of its own.
    /// </summary>
    public static IReadOnlyCollection<string> GroupReservedNames { get; } = [KeyName, RowsName];

    /// <summary>
    /// Named aggregate calls over the group, answered beside Key and Rows of the grouping's answer.
    /// </summary>
    public IReadOnlyList<GroupAggregate> Aggregates { get; init; } = [];
}
