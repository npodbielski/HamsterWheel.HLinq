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

public sealed class ThenByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser : ElementParserBase<ThenByRoot>
    {
        public override IToken[] ExampleTokens { get; } = ThenByRootExampleTokens;

        private static IToken[] ThenByRootExampleTokens =>
        [
            ThenBy.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        protected override ThenByRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [ThenBy, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [ThenBy, LeftSquareBracket, ..] => new ThenByRoot(context.Tokens[..2]),
                [Dot, ThenBy, LeftSquareBracket, ..] => new ThenByRoot(context.Tokens[..3]),
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

        private static ThenByRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                ThenByRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<ThenByRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, ThenByRoot orderBy,
            string hLinqQuery, CultureInfo culture)
        {
            if (context.Grouping is not null)
            {
                GroupedResultPropertyValidator.CheckIfPropertyAvailable(orderBy, hLinqQuery);
            }

            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery, culture,
                context.Warnings);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.ThenBy),
                infos => infos.Length == 2,
                context.CurrentResultType,
                selector.PropType);
            return context.Next(
                (IOrderedQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<ThenByRoot>
    {
        protected override Expression Build(IBuilderContext context, ThenByRoot element) =>
            context.ToExpression(element.Children[0]);
    }
}