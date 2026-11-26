using System.Globalization;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class WhereRoot(IToken[] tokens) : TreeBranch(tokens), IWhereRoot
{
    /// <summary>
    /// This should never be accessed since <see cref="WhereRoot" /> is never nested in another parent
    /// </summary>
    public ILogicalOperatorToken? LogicalOpToken => null;

    // ReSharper disable once UnusedType.Global - Used by Parser
    public sealed class Parser : OrderingOrFilteringRootParser<WhereRoot>
    {
        protected override WhereRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Where, LeftSquareBracket, ..] => new WhereRoot(context.Tokens[..2]),
                [Dot, Where, LeftSquareBracket, ..] => new WhereRoot(context.Tokens[..3]),
                _ => default
            };

        public override IToken[] ExampleTokens { get; } =
        [
            Where.Empty, LeftSquareBracket.Empty, .. Condition.Parser.ConditionExampleTokens, RightSquareBracket.Empty
        ];
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<WhereRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, WhereRoot where,
            string hLinqQuery, CultureInfo culture)
        {
            var filter = builder.GetFilter(context.CurrentResultType, where, hLinqQuery, culture,
                context.Diagnostics);

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Where),
                infos => infos[1].ParameterType.IsGenericType &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>),
                context.CurrentResultType);

            return context.NextByCalling(context.CurrentResultType,
                source => (IQueryable)method.Invoke(null, [source, filter])!, context.Count);
        }
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Converter : ConditionalLogicalOperationConverter<WhereRoot>;
}