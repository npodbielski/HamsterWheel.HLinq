using System.Linq.Expressions;
using DynamicAnonymousType;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using SelectToken = HamsterWheel.HLinq.Tokens.Selecting.Select;

namespace HamsterWheel.HLinq.Tree.Selecting;

public sealed class SelectRoot(IToken[] tokens) : TreeBranch(tokens), ISelectRoot, ITreeRoot
{
    private ITreeElement[]? _membersBindSources;

    private ITreeElement[] MembersBindSources => _membersBindSources ??=
        Children.OfType<PropertyAssignment>().Cast<ITreeElement>().ToArray();

    private bool NeedsObjectInitializer => MembersBindSources.Length > 1 ||
                                           Children.OfType<PropertyAssignment>().Any(c => c.NeedsObjectInitializer);

    public sealed class Parser : ElementParserBase<SelectRoot>
    {
        protected override SelectRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [SelectToken, LeftSquareBracket, RightSquareBracket] => ThrowOnEmptySelect(context),
                [SelectToken, LeftSquareBracket, ..] => new SelectRoot(context.Tokens[..2]),
                [Dot, SelectToken, LeftSquareBracket, ..] => new SelectRoot(context.Tokens[..3]),
                _ => null
            };

        public override IToken[] ExampleTokens => SelectRootExampleTokens;

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

        private static SelectRoot ThrowOnEmptySelect(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString,
                context.Tokens.Take(3).ToArray(), SelectRootExampleTokens);

        private static IToken[] SelectRootExampleTokens =>
        [
            SelectToken.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty, PropertyName.Empty,
            RightSquareBracket.Empty
        ];
    }

    public sealed class Applier(IExpressionBuilder builder, IMethodsCache methodsCache) : RootApplierBase<ISelectRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, ISelectRoot select,
            string hLinqQuery)
        {
            var selector = builder.GetSelect(context.CurrentResultType, select, hLinqQuery);

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Select),
                infos => infos[1].ParameterType.IsGenericType &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>), context.CurrentResultType, selector.ResultType);

            return new QueryableContext(
                (IQueryable)method.Invoke(null, [context.Queryable, selector.Expression])!,
                selector.ResultType);
        }
    }

    public sealed class Converter : ElementToExpressionConverter<SelectRoot>
    {
        protected override Expression Build(IBuilderContext context, SelectRoot element)
        {
            //select[x.Id,x.Name] or select[UserName=x.Name]
            if (element.NeedsObjectInitializer)
            {
                var properties = element.MembersBindSources.Select(context.ToPropInfo).ToArray();
                var type = DynamicFactory.CreateType(properties.Select(p => (p.Name, p.Type)));
                var memberBindings = element.MembersBindSources.Select(e =>
                    context.ToMemberAssignment(e, type)).ToArray();

                return Expression.MemberInit(Expression.New(type), memberBindings.Cast<MemberBinding>());
            }

            //select[x.Id]
            //if user wants to select with new object initializer (and json list of objects response) than it is possible to just rewrite the query to: `select[Id=x.Id]` 
            return element.MembersBindSources.Select(context.ToExpression).OfType<MemberExpression>().First();
        }
    }
}