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

namespace HamsterWheel.HLinq.Tree.Ordering;

public sealed class OrderByDescendingRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser : OrderingOrFilteringRootParser<OrderByDescendingRoot>
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

        private static OrderByDescendingRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                OrderRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache)
        : RootApplierBase<OrderByDescendingRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, OrderByDescendingRoot orderBy,
            string hLinqQuery, CultureInfo culture)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery, culture,
                context.Diagnostics);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderByDescending),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            return context.NextByCalling(context.CurrentResultType,
                source => (IQueryable)method.Invoke(null, [source, selector.Expression])!, context.Count);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<OrderByDescendingRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByDescendingRoot element) =>
            context.Builder.BuildValue(context, element.Children);
    }
}