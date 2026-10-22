using System.Globalization;
using System.Linq.Expressions;
using DynamicAnonymousType;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Grouping;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Tree.Grouping;

public sealed class GroupByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot, IProjectionMembers
{
    public IReadOnlyList<ITreeElement> MemberSources =>
        [.. Children.OfType<PropertyAssignment>()];

    public bool NeedsObjectInitializer =>
        MemberSources.Count > 1 || MemberSources.OfType<PropertyAssignment>().Any(a => a.NeedsObjectInitializer);

    public sealed class Parser : ElementParserBase<GroupByRoot>
    {
        public override IToken[] ExampleTokens => GroupByRootExampleTokens;

        private static IToken[] GroupByRootExampleTokens =>
        [
            GroupBy.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        protected override GroupByRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [GroupBy, LeftSquareBracket, RightSquareBracket] => ThrowOnEmptyGroupBy(context),
                [GroupBy, LeftSquareBracket, ..] => new GroupByRoot(context.Tokens[..2]),
                [Dot, GroupBy, LeftSquareBracket, ..] => new GroupByRoot(context.Tokens[..3]),
                _ => null
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

        private static GroupByRoot ThrowOnEmptyGroupBy(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                GroupByRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<GroupByRoot>
    {
        protected override Expression Build(IBuilderContext context, GroupByRoot element)
        {
            //groupBy[x.Status] or a composite groupBy[x.Vendor, x.Status]
            if (!element.NeedsObjectInitializer)
            {
                return element.MemberSources.Select(e => context.Builder.ToExpression(context, e))
                    .OfType<MemberExpression>().First();
            }

            var properties = element.MemberSources.Select(context.ToPropInfo).ToArray();
            var type = DynamicFactory.CreateType(properties.Select(p => (p.Name, p.Type)));
            var memberBindings = element.MemberSources
                .Select(e => context.ToMemberAssignment(e, type)).Cast<MemberBinding>();

            return Expression.MemberInit(Expression.New(type), memberBindings);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache)
        : RootApplierBase<GroupByRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, GroupByRoot groupBy,
            string hLinqQuery, CultureInfo culture)
        {
            if (context.Grouping is { } outer)
            {
                var innerKey = builder.GetProjection(outer.RowType, groupBy, hLinqQuery, culture,
                    context.Warnings);

                return context.Next(context.Queryable, context.CurrentResultType, context.Count,
                    builder.Nest(outer, innerKey.Expression));
            }

            var rowType = context.CurrentResultType;
            var key = builder.GetProjection(rowType, groupBy, hLinqQuery, culture, context.Warnings);

            var groupByMethod = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.GroupBy),
                infos => infos is [_, { ParameterType.IsGenericType: true }] &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>), rowType, key.ResultType);

            var grouping = (IQueryable)groupByMethod.Invoke(null, [context.Queryable, key.Expression])!;

            var state = StateOf(grouping, rowType, key.ResultType);

            return context.Next(grouping, state.GroupingItemType, grouping: state);
        }
    }

    private static GroupingState StateOf(IQueryable grouping, Type rowType, Type keyType)
    {
        var groupingItemType = typeof(IGrouping<,>).MakeGenericType(keyType, rowType);
        var groupParam = Expression.Parameter(groupingItemType, "g");
        var keyMember = Expression.Property(groupParam, GroupingState.KeyName);
        var rowParam = Expression.Parameter(rowType, "row");
        var rowSelector = Expression.Lambda(typeof(Func<,>).MakeGenericType(rowType, rowType), rowParam, rowParam);
        return new GroupingState(grouping, rowType, keyType, groupingItemType, groupParam, keyMember,
            rowSelector, rowType);
    }
}