using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Tree.Ordering;

public sealed class OrderByDescendingRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public sealed class Parser : ElementParserBase<OrderByDescendingRoot>
    {
        public override IToken[] ExampleTokens { get; } = OrderRootExampleTokens;

        private static IToken[] OrderRootExampleTokens =>
        [
            OrderByDescending.Empty, LeftSquareBracket.Empty,
            Entity.Empty, Dot.Empty, PropertyName.Empty, RightSquareBracket.Empty
        ];

        protected override OrderByDescendingRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [OrderByDescending, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [OrderByDescending, LeftSquareBracket, ..] => new OrderByDescendingRoot(context.Tokens[..2]),
                [Dot, OrderByDescending, LeftSquareBracket, ..] => new OrderByDescendingRoot(context.Tokens[..3]),
                _ => default
            };
        }

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightSquareBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }

        private static OrderByDescendingRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                OrderRootExampleTokens);
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache)
        : RootApplierBase<OrderByDescendingRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, OrderByDescendingRoot orderBy,
            string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderByDescending),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }

    public sealed class Converter : ElementToExpressionConverter<OrderByDescendingRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByDescendingRoot element) =>
            context.ToExpression(element.Children[0]);
    }
}