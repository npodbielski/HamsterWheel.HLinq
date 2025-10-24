using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class WhereRoot(IToken[] tokens) : TreeBranch(tokens), IWhereRoot
{
    /// <summary>
    ///     This should never be accessed since <see cref="WhereRoot" /> is never nested in another parent
    /// </summary>
    public ILogicalOperatorToken? LogicalOpToken => null;

    public sealed class Parser : ElementParserBase<WhereRoot>
    {
        protected override WhereRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Where, LeftSquareBracket, ..] => new WhereRoot(context.Tokens[..2]),
                [Dot, Where, LeftSquareBracket, ..] => new WhereRoot(context.Tokens[..3]),
                _ => default
            };

        public override IToken[] ExampleTokens { get; } =
            [Where.Empty, LeftSquareBracket.Empty, ..Condition.Parser.ConditionExampleTokens, RightSquareBracket.Empty];

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens.Take(5).ToArray(),
                    [new RightSquareBracket()]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveTokensFromStart(1);
        }
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<WhereRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, WhereRoot where,
            string hLinqQuery)
        {
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Where),
                infos => infos[1].ParameterType.IsGenericType &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>),
                context.CurrentResultType);

            var filter = builder.GetFilter(context.CurrentResultType, where, hLinqQuery);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, filter])!,
                context.CurrentResultType, context.Count);
        }
    }

    public sealed class Converter : ConditionalLogicalOperationConverter<WhereRoot>;
}