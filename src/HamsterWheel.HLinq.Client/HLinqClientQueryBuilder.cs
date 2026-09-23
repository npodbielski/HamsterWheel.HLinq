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
    ///     Adds a <c>take[start..end]</c> range, i.e. <c>Take(10..20)</c> builds <c>take[10..20]</c>
    ///     (equivalent of <c>skip[10].take[10]</c>). An open end (<c>Take(10..)</c>) builds
    ///     <c>take[10..]</c> which, like a bare <c>skip</c>, is not capped at the maximum take.
    ///     Indexes counted from the end (<c>^</c>) are not supported. Note that C# cannot tell
    ///     an open-ended <c>..</c> range apart from <c>0..0</c>, and a <c>0</c> end always builds
    ///     an open end.
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
}