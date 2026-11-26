using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public abstract class ConditionalLogicalOperationConverter<TBranch> :
    ElementToExpressionConverter<TBranch> where TBranch : ILogicalOperationGroupBranch
{
    protected override Expression Build(IBuilderContext context, TBranch parent)
    {
        var diagnostics = context.Diagnostics;
        var refused = false;
        Expression? body = null;

        foreach (var element in parent.Children)
        {
            if (element is not ILogicalOperationGroupBranch branch)
            {
                continue;
            }

            Expression innerBody;
            try
            {
                innerBody = context.ToExpression(branch);
            }
            catch (HLinqQueryException exception) when (diagnostics is not null)
            {
                diagnostics.AddException(exception);
                refused = true;
                continue;
            }

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

        if (refused)
        {
            throw new ConditionGroupRefusedException();
        }

        return body ?? Expression.Constant(true);
    }

    private sealed class ConditionGroupRefusedException()
        : HLinqQueryException("Condition query was invalid and could not be translated to expression")
    {
        public override bool ShouldReportToClient => false;
    }
}
