using System.Linq.Expressions;
using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filter;

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
                if (branch.ConditionalLogicalOp is not null)
                    body = branch.ConditionalLogicalOp switch
                    {
                        And => Expression.AndAlso(body, innerBody),
                        Or => Expression.OrElse(body, innerBody),
                        _ => throw new NotSupportedException(
                            $"{branch.ConditionalLogicalOp} is not supported!")
                    };
                else
                    throw new InvalidOperationException(
                        $"LogicOp need to be provided in any {nameof(Condition)} or {nameof(ConditionGroup)} beside the first one");
            }
        }

        return body ?? Expression.Constant(true);
    }
}