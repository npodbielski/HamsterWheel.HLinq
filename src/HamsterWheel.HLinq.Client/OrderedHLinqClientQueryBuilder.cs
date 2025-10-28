using System.Linq.Expressions;

namespace HamsterWheel.HLinq.Client;

public class OrderedHLinqClientQueryBuilder<T> : HLinqClientQueryBuilder<T>
{
    public OrderedHLinqClientQueryBuilder<T> ThenBy(Expression<Func<T, object>> selector)
    {
        var expression = selector.Body;
        if (expression is UnaryExpression ue)
        {
            expression = ue.Operand;
        }

        if (expression is MemberExpression me)
        {
            AddDotIfNecessary();
            Query.Append("thenBy[").Append(me).Append(']');
        }

        return NextOrdered<T>(this);
    }

    public OrderedHLinqClientQueryBuilder<T> ThenByDescending(Expression<Func<T, object>> selector)
    {
        var expression = selector.Body;
        if (expression is UnaryExpression ue)
        {
            expression = ue.Operand;
        }

        if (expression is MemberExpression me)
        {
            AddDotIfNecessary();
            Query.Append("thenByDescending[").Append(me).Append(']');
        }

        return NextOrdered<T>(this);
    }
}