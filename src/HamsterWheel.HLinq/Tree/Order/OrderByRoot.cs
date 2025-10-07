using System.Linq.Expressions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tokens.Order;

namespace HamsterWheel.HLinq.Tree.Order;

public sealed class OrderByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public Property[] Props => GetAll<Property>().ToArray();

    public class Parser : ElementParserBase<OrderByRoot>
    {
        protected override OrderByRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [OrderBy, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [OrderBy, LeftSquareBracket, ..] => new OrderByRoot(context.Tokens[..2]),
                [Dot, OrderBy, LeftSquareBracket, ..] => new OrderByRoot(context.Tokens[..3]),
                _ => default
            };
        }

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is [RightSquareBracket bracket, ..])
            {
                context.CurrentBranch?.Finish(context, [bracket]);
                context.RemoveTokensFromStart(1);
                return;
            }

            //This should contain surrounding tokens, query or whole hLinq query
            throw new InvalidTokenCollectionException(context.Tokens.Take(5).ToArray(),
                [new RightSquareBracket(default)]);
        }

        private static OrderByRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(
                context.Tokens.Take(3).ToArray(), [
                    new OrderBy(default), new LeftSquareBracket(default),
                    new Entity(default), new PropertyAccess(default), new RightSquareBracket(default)
                ]);
    }

    public sealed class Converter : ElementToExpressionConverter<OrderByRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByRoot element) => context.ToExpression(element.Children[0]);
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<OrderByRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, OrderByRoot orderBy, string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderBy),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            //TODO: reuse delegate helper package
            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }
}