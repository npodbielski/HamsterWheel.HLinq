using System.Linq.Expressions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Grouping;

namespace HamsterWheel.HLinq.Tree.Grouping;

/// <summary>
/// Tree node representing a <c>groupBy[x.Property]</c> root stage in an HLinq query.
/// Groups the source sequence by the specified key selector.
/// </summary>
public sealed class GroupByRoot(IToken[] tokens) : TreeBranch(tokens), IGroupByRoot
{
    /// <summary>Parses a <c>groupBy[…]</c> token sequence into a <see cref="GroupByRoot"/> branch.</summary>
    public sealed class Parser : ElementParserBase<GroupByRoot>
    {
        private static IToken[] GroupByRootExampleTokens =>
        [
            GroupBy.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        /// <inheritdoc/>
        public override IToken[] ExampleTokens { get; } = GroupByRootExampleTokens;

        /// <inheritdoc/>
        protected override GroupByRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [GroupBy, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [GroupBy, LeftSquareBracket, ..] => new GroupByRoot(context.Tokens[..2]),
                [Dot, GroupBy, LeftSquareBracket, ..] => new GroupByRoot(context.Tokens[..3]),
                _ => null
            };

        /// <inheritdoc/>
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

        private static GroupByRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                GroupByRootExampleTokens);
    }

    /// <summary>
    /// Applies the <c>groupBy</c> operation to the current <see cref="IQueryable"/>, producing
    /// an <c>IQueryable&lt;IGrouping&lt;TKey, TSource&gt;&gt;</c>.
    /// </summary>
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<GroupByRoot>
    {
        /// <inheritdoc/>
        protected override IQueryableContext ApplyImpl(IQueryableContext context, GroupByRoot groupBy,
            string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, groupBy, hLinqQuery);

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.GroupBy),
                infos => infos.Length == 2, context.CurrentResultType, selector.PropType);

            var groupedType = typeof(IGrouping<,>).MakeGenericType(selector.PropType, context.CurrentResultType);

            return new QueryableContext(
                (IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                groupedType, context.Count);
        }
    }

    /// <summary>Converts a <see cref="GroupByRoot"/> to a key-selector expression.</summary>
    public sealed class Converter : ElementToExpressionConverter<GroupByRoot>
    {
        /// <inheritdoc/>
        protected override Expression Build(IBuilderContext context, GroupByRoot element) =>
            context.ToExpression(element.Children[0]);
    }
}
