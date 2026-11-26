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

public sealed class OrderByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    // ReSharper disable once UnusedType.Global - Used by Parser
    public class Parser : OrderingOrFilteringRootParser<OrderByRoot>
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
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery, culture,
                context.Diagnostics);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.OrderBy),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);
            return context.NextByCalling(context.CurrentResultType,
                source => (IQueryable)method.Invoke(null, [source, selector.Expression])!, context.Count);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<OrderByRoot>
    {
        protected override Expression Build(IBuilderContext context, OrderByRoot element) =>
            context.Builder.BuildValue(context, element.Children);
    }
}