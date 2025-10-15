using System.Linq.Expressions;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree;

namespace HamsterWheel.HLinq.Builders;

public abstract class ConditionalLogicalOperationConverter<TBranch> :
    ElementToExpressionConverter<TBranch> where TBranch : ILogicalOperationGroupBranch
{
    protected override Expression Build(IBuilderContext context, TBranch parent)
    {
        Expression? body = null;
        var enumerable = parent.Children;
        foreach (var element in enumerable)
        {
            if (element is not ILogicalOperationGroupBranch branch)
            {
                continue;
            }

            var innerBody = context.ToExpression(branch);
            if (body is null)
            {
                body = innerBody;
            }
            else
            {
                if (branch.LogicalOpToken is not null)
                {
                    body = branch.LogicalOpToken switch
                    {
                        And => Expression.AndAlso(body, innerBody),
                        Or => Expression.OrElse(body, innerBody),
                        _ => throw new InvalidConditionalLogicalOperationException(branch.LogicalOpToken)
                    };
                }
                else
                {
                    throw new ConditionalLogicalOperationTokenCannotBeFirstException();
                }
            }
        }

        return body ?? Expression.Constant(true);
    }
}