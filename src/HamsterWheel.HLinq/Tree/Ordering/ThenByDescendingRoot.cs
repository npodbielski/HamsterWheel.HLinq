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

public sealed class ThenByDescendingRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser : OrderingOrFilteringRootParser<ThenByDescendingRoot>
    {
        public override IToken[] ExampleTokens { get; } = ThenByDescendingRootExampleTokens;

        private static IToken[] ThenByDescendingRootExampleTokens =>
        [
            ThenByDescending.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        protected override ThenByDescendingRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [ThenByDescending, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [ThenByDescending, LeftSquareBracket, ..] => new ThenByDescendingRoot(context.Tokens[..2]),
                [Dot, ThenByDescending, LeftSquareBracket, ..] => new ThenByDescendingRoot(context.Tokens[..3]),
                _ => default
            };

        private static ThenByDescendingRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                ThenByDescendingRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache)
        : RootApplierBase<ThenByDescendingRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context,
            ThenByDescendingRoot orderBy, string hLinqQuery, CultureInfo culture)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery, culture,
                context.Diagnostics);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.ThenByDescending),
                infos => infos.Length == 2,
                context.CurrentResultType, selector.PropType);
            return context.NextByCalling(context.CurrentResultType,
                source => (IQueryable)method.Invoke(null, [source, selector.Expression])!, context.Count);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<ThenByDescendingRoot>
    {
        protected override Expression Build(IBuilderContext context, ThenByDescendingRoot element) =>
            context.Builder.BuildValue(context, element.Children);
    }
}