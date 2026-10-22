using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using HamsterWheel.HLinq.Pipeline.Applier;

namespace HamsterWheel.HLinq.Client;

/// <summary>
/// The answer to a grouping: a list of {Key, Rows} entries, one per group - the same shape whether
/// the grouping named one key or several, and the key keeps its own type in the answer.
/// </summary>
public class GroupedHLinqClientQueryBuilder<TKey, TRow>
    : ResponseHLinqClientQueryBuilder<List<Group<TKey, TRow>>> where TKey : notnull
{
    public GroupedHLinqClientQueryBuilder(JsonSerializerOptions? options = null, string query = "")
        : base(options) => Query = new StringBuilder(query);

    /// <summary>
    /// Names the shape of the rows of each group. It has to be written directly after the grouping:
    /// anything applied between the two is applied to groups, not to rows.
    /// </summary>
    public GroupedHLinqClientQueryBuilder<TKey, TNew> Select<TNew>(
        Expression<Func<TRow, TNew>> selector)
    {
        switch (selector.Body)
        {
            case MemberExpression me:
                AddDotIfNecessary();
                Query.Append("select[").Append(me).Append(']');
                break;
            case NewExpression { Members: not null } ne:
                AddDotIfNecessary();
                Query.Append("select[").Append(ne.ToString().Split('(', ')')[1]).Append(']');
                break;
        }

        return new GroupedHLinqClientQueryBuilder<TKey, TNew>(JsonSerializerOptions, Query.ToString());
    }

    /// <summary>
    /// Keeps this many groups - not this many rows.
    /// </summary>
    public GroupedHLinqClientQueryBuilder<TKey, TRow> Take(int take)
    {
        AddDotIfNecessary();
        Query.Append($"take[{take}]");
        return this;
    }

    /// <summary>
    /// Skips this many groups - not this many rows.
    /// </summary>
    public GroupedHLinqClientQueryBuilder<TKey, TRow> Skip(int skip)
    {
        AddDotIfNecessary();
        Query.Append($"skip[{skip}]");
        return this;
    }

    /// <summary>
    /// Orders the groups by their key. Only the key orders the groups; the rows of a group cannot be
    /// ordered or filtered by from here.
    /// </summary>
    public GroupedHLinqClientQueryBuilder<TKey, TRow> OrderByKey(bool descending = false)
    {
        AddDotIfNecessary();
        Query.Append(descending ? "orderByDescending[x.Key]" : "orderBy[x.Key]");
        return this;
    }

    /// <summary>
    /// Keeps only the groups whose key the predicate accepts. Only the key filters the groups; the
    /// rows of a group cannot be filtered by from here.
    /// </summary>
    public GroupedHLinqClientQueryBuilder<TKey, TRow> WhereKey(Expression<Func<TKey, bool>> predicate)
    {
        if (predicate.Body is not BinaryExpression { Right: ConstantExpression or MemberExpression } binary
            || predicate.Body is not BinaryExpression { Left: ParameterExpression or MemberExpression })
        {
            throw new ArgumentException(
                "a key of a group is filtered by comparing it with a value, i.e. k => k == \"Paid\"",
                nameof(predicate));
        }

        var value = Expression.Lambda(binary.Right).Compile().DynamicInvoke();
        AddDotIfNecessary();
        Query.Append("where[x.Key==").Append(Quote(value)).Append(']');
        return this;
    }

    private static string Quote(object? value) =>
        value is string s ? $"\"{s}\"" : value?.ToString() ?? "null";

    /// <summary>
    /// How many groups the answer has - not how many rows stand under its keys.
    /// </summary>
    public ResponseHLinqClientQueryBuilder<int> Count()
    {
        AddDotIfNecessary();
        Query.Append("count[]");
        return NextCounted(this);
    }
}
