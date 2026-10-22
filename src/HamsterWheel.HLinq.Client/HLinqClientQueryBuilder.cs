using System.Linq.Expressions;
using System.Text.Json;
using HamsterWheel.HLinq.Client.RootBuilders;

namespace HamsterWheel.HLinq.Client;

public class HLinqClientQueryBuilder<T> : ResponseHLinqClientQueryBuilder<T[]>
{
    protected HLinqClientQueryBuilder(JsonSerializerOptions? jsonSerializerOptions = null) : base(jsonSerializerOptions)
    {
    }

    public UnorderedHLinqClientQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        switch (selector.Body)
        {
            case MemberExpression me:
                AddDotIfNecessary();
                Query.Append("select[").Append(me).Append(']');
                break;
            case NewExpression ne:
                {
                    var m = ne.Members;
                    if (m is not null)
                    {
                        AddDotIfNecessary();
                        Query.Append("select[").Append(ne.ToString().Split('(', ')')[1]).Append(']');
                    }

                    break;
                }
        }

        return Next<TResult>(this);
    }

    public UnorderedHLinqClientQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        AddDotIfNecessary();
        new WhereRootBuilder<T>(Query).Build(predicate);
        return Next<T>(this);
    }

    public UnorderedHLinqClientQueryBuilder<T> Skip(int skip)
    {
        AddDotIfNecessary();
        Query.Append($"skip[{skip}]");
        return Next<T>(this);
    }

    public UnorderedHLinqClientQueryBuilder<T> Take(int skip)
    {
        AddDotIfNecessary();
        Query.Append($"take[{skip}]");
        return Next<T>(this);
    }

    /// <summary>
    /// Takes a specified range of rows from a collection
    /// </summary>
    public UnorderedHLinqClientQueryBuilder<T> Take(Range range)
    {
        if (range.Start.IsFromEnd || range.End.IsFromEnd && !range.End.Equals(Index.End))
        {
            throw new ArgumentException(
                "take[start..end] range indexes counted from the end are not supported", nameof(range));
        }

        AddDotIfNecessary();
        Query.Append("take[").Append(range.Start.Value).Append("..");

        // `10..` and `10..^0` have no explicit end - emit `take[10..]`, `..20` comes in as `0..20`
        if (!range.End.IsFromEnd && !range.End.Equals(Index.Start))
        {
            Query.Append(range.End.Value);
        }

        Query.Append(']');
        return Next<T>(this);
    }

    public ResponseHLinqClientQueryBuilder<int> Count()
    {
        AddDotIfNecessary();
        Query.Append("count[]");
        return NextCounted(this);
    }

    /// <summary>
    /// Groups the rows by one property: the answer is a list of Group(Key, Rows) entries, one per group. What follows the grouping works on groups - <c>Take</c> keeps groups, <c>Count</c>
    /// answers how many groups there are - except a <c>Select</c> written directly after it, which
    /// names the shape of the rows and may read <c>x.Key</c>, the key of the group a row is in.
    /// </summary>
    public GroupedHLinqClientQueryBuilder<TKey, T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        where TKey : notnull
    {
        if (keySelector.Body is not MemberExpression me)
        {
            throw new ArgumentException(
                "groupBy takes a property of the entity, i.e. x => x.Status", nameof(keySelector));
        }

        AddDotIfNecessary();
        Query.Append("groupBy[").Append(me).Append(']');
        return new GroupedHLinqClientQueryBuilder<TKey, T>(JsonSerializerOptions, Query.ToString());
    }
}