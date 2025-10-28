using System.Linq.Expressions;
using System.Text.Json;

namespace HamsterWheel.HLinq.Client;

public class UnorderedHLinqClientQueryBuilder<T>(JsonSerializerOptions? jsonSerializerOptions = null)
    : HLinqClientQueryBuilder<T>(jsonSerializerOptions)
{
    public OrderedHLinqClientQueryBuilder<T> OrderBy(Expression<Func<T, object?>> selector)
    {
        var expression = selector.Body;
        if (expression is UnaryExpression ue)
        {
            expression = ue.Operand;
        }

        if (expression is MemberExpression me)
        {
            AddDotIfNecessary();
            Query.Append("orderBy[").Append(me).Append(']');
        }

        return NextOrdered<T>(this);
    }

    public OrderedHLinqClientQueryBuilder<T> OrderByDescending(Expression<Func<T, object>> selector)
    {
        var expression = selector.Body;
        if (expression is UnaryExpression ue)
        {
            expression = ue.Operand;
        }

        if (expression is MemberExpression me)
        {
            AddDotIfNecessary();
            Query.Append("orderByDescending[").Append(me).Append(']');
        }

        return NextOrdered<T>(this);
    }
}