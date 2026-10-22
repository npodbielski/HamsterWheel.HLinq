using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Pipeline.Applier;

/// <summary>
/// Group state data
/// </summary>
public sealed record GroupingState(
    IQueryable Grouping,
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
    /// If group is inside another group.
    /// </summary>
    public bool IsNested => InnerKey is not null;

    /// <summary>
    /// The name a grouped row context reads the key of its group under.
    /// </summary>
    public const string KeyName = "Key";

    /// <summary>
    /// Name of property that holds group rows
    /// </summary>
    public const string RowsName = "Rows";
}
