using System.Linq.Expressions;
using DynamicAnonymousType;
using System.Globalization;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree.Grouping;
using SelectToken = HamsterWheel.HLinq.Tokens.Selecting.Select;

namespace HamsterWheel.HLinq.Tree.Selecting;

public sealed class SelectRoot(IToken[] tokens) : TreeBranch(tokens), ISelectRoot, ITreeRoot, IProjectionMembers
{
    public sealed class Parser : ElementParserBase<SelectRoot>
    {
        public override IToken[] ExampleTokens => SelectRootExampleTokens;

        private static IToken[] SelectRootExampleTokens =>
        [
            SelectToken.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];

        protected override SelectRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [SelectToken, LeftSquareBracket, RightSquareBracket] => ThrowOnEmptySelect(context),
                [SelectToken, LeftSquareBracket, ..] => new SelectRoot(context.Tokens[..2]),
                [Dot, SelectToken, LeftSquareBracket, ..] => new SelectRoot(context.Tokens[..3]),
                _ => null
            };

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightSquareBracket.Empty]);
            }

            //select after group rewrites projection
            var siblings = context.Parents.Peek().Children;
            if (siblings.Count(root => root is GroupByRoot) > 0 && siblings[^2] is not GroupByRoot)
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [Dot.Empty, SelectToken.Empty, LeftSquareBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }

        private static SelectRoot ThrowOnEmptySelect(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                SelectRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<ISelectRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, ISelectRoot select,
            string hLinqQuery, CultureInfo culture)
        {
            var aggregateAssignments = select.AggregateAssignments;

            if (context.Grouping is { } grouping)
            {
                var state = grouping;
                if (aggregateAssignments.Count > 0)
                {
                    var aggregates = new List<GroupAggregate>();
                    foreach (var assignment in aggregateAssignments)
                    {
                        aggregates.Add(new GroupAggregate(
                            assignment.AggregateName(hLinqQuery) ?? assignment.Call!.GetName(hLinqQuery),
                            builder.GroupAggregateSelector(grouping, assignment.Call!, hLinqQuery, culture,
                                context.Warnings)));
                    }

                    state = grouping with { Aggregates = aggregates };
                }

                if (select.RowMemberSources.Count == 0)
                {
                    return context.Next(context.Queryable, grouping.GroupingItemType, context.Count, state);
                }

                var (rowSelector, rowShape) = builder.GroupRowSelector(state, select, hLinqQuery, culture,
                    context.Warnings);

                return context.Next(context.Queryable, grouping.GroupingItemType,
                    context.Count,
                    state with { RowSelector = rowSelector, RowShape = rowShape });
            }

            if (aggregateAssignments.Count > 0)
            {
                throw new EntityAggregateNeedsGroupException(
                    aggregateAssignments[0].Call!.GetName(hLinqQuery));
            }

            var selector = builder.GetSelect(context.CurrentResultType, select, hLinqQuery, culture,
                context.Warnings);

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Select),
                infos => infos[1].ParameterType.IsGenericType &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>), context.CurrentResultType, selector.ResultType);

            return context.Next(
                (IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                selector.ResultType);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ElementToExpressionConverter<SelectRoot>
    {
        protected override Expression Build(IBuilderContext context, SelectRoot element)
        {
            //select[x.id,x.Name] or select[UserName=x.Name]
            if (!element.NeedsObjectInitializer)
            {
                return element.MemberSources.Select(e => context.Builder.ToExpression(context, e))
                    .OfType<MemberExpression>().First();
            }

            //aggregates over the group are answered by the grouping's projection, never in a row
            var members = element.RowMemberSources;
            var properties = members.Select(context.ToPropInfo).ToArray();
            var type = DynamicFactory.CreateType(properties.Select(p => (p.Name, p.Type)));
            var memberBindings = members
                .Select(e => context.ToMemberAssignment(e, type)).Cast<MemberBinding>();

            return Expression.MemberInit(Expression.New(type), memberBindings);
        }
    }
}