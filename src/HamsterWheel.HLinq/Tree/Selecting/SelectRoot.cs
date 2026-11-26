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
    public INestedSelect? RowsSelection { get; private set; }

    /// <summary>
    /// Whether a <c>groupBy</c> was committed before this select.
    /// </summary>
    public bool StandsOnGroups { get; private set; }
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
            var select = (SelectRoot)context.CurrentElement;
            select.StandsOnGroups = context.GroupingApplied;
            if (context.GroupingApplied)
            {
                ValidateRowsSelectNames(select, context.SourceQueryString);
                select.RowsSelection = ResolveRowsSelection(select, context.SourceQueryString);
            }

            select.Children.RefuseDuplicateSelects(context.SourceQueryString);

            context.RemoveStartTokens(1);
        }

        /// <summary>
        /// Refuse a member whose name the grouped answer already holds, or which two members take.
        /// </summary>
        private static void ValidateRowsSelectNames(SelectRoot select, string hLinqQuery)
        {
            var taken = new HashSet<string>(GroupingState.GroupReservedNames, StringComparer.OrdinalIgnoreCase);

            foreach (var assignment in select.AggregateAssignments)
            {
                var name = assignment.FinalPropertyName(hLinqQuery);
                if (name is not null && !taken.Add(name))
                {
                    throw new GroupAggregateNameCollisionException(name);
                }
            }
        }

        private static INestedSelect? ResolveRowsSelection(SelectRoot select, string hLinqQuery)
        {
            IPropertyAssignment? rowsSelection = null;
            foreach (var member in select.MemberSources)
            {
                if (member.GroupAggregate is not null)
                {
                    continue;
                }

                var written = member.ExplicitPropertyName(hLinqQuery);
                if (written is null || !string.Equals(written, GroupingState.RowsName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var source = member.NestedSelectProperty?.GetPath(hLinqQuery);
                var namesTheGroupsRows = source is [var only]
                                         && string.Equals(only, GroupingState.RowsName,
                                             StringComparison.OrdinalIgnoreCase);
                if (!namesTheGroupsRows)
                {
                    throw member.NestedSelectProperty is null
                        ? new GroupRowsNameReservedException(written)
                        : new GroupRowsSelectSourceException(written);
                }

                if (rowsSelection is not null)
                {
                    throw new GroupRowsSelectedTwiceException();
                }

                rowsSelection = member;
            }

            if (rowsSelection is not null && select.RowMemberSources.Any(m => !ReferenceEquals(m, rowsSelection)))
            {
                throw new GroupRowsSelectedTwiceException();
            }

            return rowsSelection?.NestedSelect;
        }

        private static SelectRoot ThrowOnEmptySelect(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                SelectRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<SelectRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, SelectRoot select,
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
                            assignment.FinalPropertyName(hLinqQuery)!,
                            builder.GroupAggregateSelector(grouping, assignment.GroupAggregate!, hLinqQuery,
                                culture, context.Diagnostics)));
                    }

                    state = grouping with { Aggregates = aggregates };
                }

                if (select.RowMemberSources.Count == 0)
                {
                    return context.Next(context.Queryable, grouping.GroupingItemType, context.Count, state);
                }

                var (rowSelector, rowShape) = builder.GroupRowSelector(state, select, hLinqQuery, culture,
                    context.Diagnostics);

                return context.Next(context.Queryable, grouping.GroupingItemType,
                    context.Count,
                    state with { RowSelector = rowSelector, RowShape = rowShape });
            }

            var selector = builder.GetSelect(context.CurrentResultType, select, hLinqQuery, culture,
                context.Diagnostics);

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Select),
                infos => infos[1].ParameterType.IsGenericType &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>), context.CurrentResultType, selector.ResultType);

            return context.NextByCalling(selector.ResultType,
                source => (IQueryable)method.Invoke(null, [source, selector.Expression])!);
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