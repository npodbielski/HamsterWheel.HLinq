using System.Linq.Expressions;
using System.Text;

namespace HamsterWheel.HLinq.Client.RootBuilders;

public class GroupByRootBuilder<T>(StringBuilder query)
{
    public void Build<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var expression = keySelector.Body;
        if (expression is UnaryExpression ue)
        {
            expression = ue.Operand;
        }

        query.Append("groupby[");
        query.Append(expression);
        query.Append(']');
    }
}
