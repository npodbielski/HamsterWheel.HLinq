using System.Linq.Expressions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Order;

namespace HamsterWheel.HLinq.Tree.Order;

public sealed class OrderByDescendingRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public sealed class Parser : ElementParserBase<OrderByDescendingRoot>
    {
        protected override OrderByDescendingRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [OrderByDescending, LeftSquareBracket, ..] => new OrderByDescendingRoot(context.Tokens[..2]),
                [Dot, OrderByDescending, LeftSquareBracket, ..] => new OrderByDescendingRoot(context.Tokens[..3]),
                _ => default
            };
        }

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                //This should contain surrounding tokens, query or whole hLinq query
                throw new InvalidTokenCollectionException(context.Tokens.Take(5).ToArray(),
                    [new RightSquareBracket(default)]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveTokensFromStart(1);
        }
    }

    public sealed class Converter : ElementToExpressionConverter<OrderByDescendingRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByDescendingRoot element) =>
            context.ToExpression(element.Children[0]);
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache)
        : RootApplierBase<OrderByDescendingRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context,
            OrderByDescendingRoot orderBy,
            string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderByDescending),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }
}