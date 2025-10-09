using System.Linq.Expressions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;

namespace HamsterWheel.HLinq.Tree.Ordering;

public sealed class ThenByRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public sealed class Parser : ElementParserBase<ThenByRoot>
    {
        public override IToken[] ExampleTokens { get; } = ThenByRootExampleTokens;
        
        protected override ThenByRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [ThenBy, LeftSquareBracket, RightSquareBracket] => ThrowOnEmptySelect(context),
                [ThenBy, LeftSquareBracket, ..] => new ThenByRoot(context.Tokens[..2]),
                [Dot, ThenBy, LeftSquareBracket, ..] => new ThenByRoot(context.Tokens[..3]),
                _ => default
            };
        }

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString,context.Tokens.Take(5).ToArray(),
                    [new RightSquareBracket(default)]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveTokensFromStart(1);
        }

        private static ThenByRoot ThrowOnEmptySelect(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString,
                context.Tokens.Take(3).ToArray(), ThenByRootExampleTokens);

        private static IToken[] ThenByRootExampleTokens =>
        [
            new ThenBy(default), new LeftSquareBracket(default),
            new Entity(default), new Dot(default), new PropertyAccess(default), new RightSquareBracket(default)
        ];
    }

    public sealed class Converter : ElementToExpressionConverter<ThenByRoot>
    {
        protected override Expression Build(IBuilderContext context, ThenByRoot element) => context.ToExpression(element.Children[0]);
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<ThenByRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, ThenByRoot orderBy,
            string hLinqQuery)
        {
            var selector = builder.GetProperty(context.CurrentResultType, orderBy, hLinqQuery);
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.ThenBy),
                infos => infos.Length == 2,
                context.CurrentResultType,
                selector.PropType);
            return new QueryableContext(
                (IOrderedQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                context.CurrentResultType, context.Count);
        }
    }
}