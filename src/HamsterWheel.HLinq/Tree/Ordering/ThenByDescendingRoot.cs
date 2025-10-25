using System.Linq.Expressions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Tree.Ordering;

public sealed class ThenByDescendingRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public sealed class Parser : ElementParserBase<ThenByDescendingRoot>
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
                [ThenByDescending, LeftSquareBracket, RightSquareBracket] => ThrowOnEmptySelect(context),
                [ThenByDescending, LeftSquareBracket, ..] => new ThenByDescendingRoot(context.Tokens[..2]),
                [Dot, ThenByDescending, LeftSquareBracket, ..] => new ThenByDescendingRoot(context.Tokens[..3]),
                _ => default
            };

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens.Take(5).ToArray(),
                    [RightSquareBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }

        private static ThenByDescendingRoot ThrowOnEmptySelect(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString,
                context.Tokens.Take(3).ToArray(), ThenByDescendingRootExampleTokens);
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache)
        : RootApplierBase<ThenByDescendingRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context,
            ThenByDescendingRoot orderBy, string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.ThenByDescending),
                infos => infos.Length == 2,
                context.CurrentResultType, selector.PropType);
            return new QueryableContext(
                (IOrderedQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }

    public sealed class Converter : ElementToExpressionConverter<ThenByDescendingRoot>
    {
        protected override Expression Build(IBuilderContext context, ThenByDescendingRoot element) =>
            context.ToExpression(element.Children[0]);
    }
}