using System.Globalization;
using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tree.Grouping;

namespace HamsterWheel.HLinq.Tree.Ordering;

public sealed class OrderByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    // ReSharper disable once UnusedType.Global - Used by Parser
    public class Parser : ElementParserBase<OrderByRoot>
    {
        public override IToken[] ExampleTokens { get; } = OrderByRootExampleTokens;

        private static IToken[] OrderByRootExampleTokens =>
        [
            OrderBy.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        protected override OrderByRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [OrderBy, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [OrderBy, LeftSquareBracket, ..] => new OrderByRoot(context.Tokens[..2]),
                [Dot, OrderBy, LeftSquareBracket, ..] => new OrderByRoot(context.Tokens[..3]),
                _ => default
            };

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

        private static OrderByRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                OrderByRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<OrderByRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, OrderByRoot orderBy,
            string hLinqQuery, CultureInfo culture)
        {
            if (context.Grouping is not null)
            {
                GroupedResultPropertyValidator.CheckIfPropertyAvailable(orderBy, hLinqQuery);
            }

            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery, culture,
                context.Warnings);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderBy),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            return context.Next((IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<OrderByRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByRoot element) =>
            context.ToExpression(element.Children[0]);
    }
}