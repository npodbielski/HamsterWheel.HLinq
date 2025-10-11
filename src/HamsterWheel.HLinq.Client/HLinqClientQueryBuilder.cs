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

    public ResponseHLinqClientQueryBuilder<int> Count()
    {
        AddDotIfNecessary();
        Query.Append("count[]");
        return NextCounted(this);
    }
}