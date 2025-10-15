using System.Linq.Expressions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Tree.Ordering;

public sealed class OrderByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
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

        public override IToken[] ExampleTokens { get; } = OrderByRootExampleTokens;

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens.Take(5).ToArray(),
                    [new RightSquareBracket(default)]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveTokensFromStart(1);
        }

        private static OrderByRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString,
                context.Tokens.Take(3).ToArray(), OrderByRootExampleTokens);

        private static IToken[] OrderByRootExampleTokens =>
        [
            new OrderBy(default), new LeftSquareBracket(default),
            new Entity(default), new Dot(default), new PropertyAccess(default), new RightSquareBracket(default)
        ];
    }

    public sealed class Converter : ElementToExpressionConverter<OrderByRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByRoot element) =>
            context.ToExpression(element.Children[0]);
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<OrderByRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, OrderByRoot orderBy, string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderBy),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }
}