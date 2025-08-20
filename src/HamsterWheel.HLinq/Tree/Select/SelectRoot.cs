using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Tokens;
using DynamicAnonymousType;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens.Filter;
using SelectToken = HamsterWheel.HLinq.Tokens.Select.Select;

namespace HamsterWheel.HLinq.Tree.Select;

public sealed class SelectRoot(IToken[] tokens) : TreeBranch(tokens), ISelectRoot, ITreeRoot
{
    private ITreeElement[]? _membersBindSources;

    private ITreeElement[] MembersBindSources => _membersBindSources ??=
        Children.OfType<PropertyAssignment>().Cast<ITreeElement>().ToArray();

    private bool NeedsObjectInitializer => MembersBindSources.Length > 1 ||
                                           Children.OfType<PropertyAssignment>().Any(c => c.NeedsObjectInitializer);

    public sealed class Parser : ElementParserBase<SelectRoot>
    {
        protected override SelectRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [SelectToken, LeftSquareBracket, RightSquareBracket] => ThrowOnEmptySelect(context),
                [SelectToken, LeftSquareBracket, ..] => new SelectRoot(context.Tokens[..2]),
                [Dot, SelectToken, LeftSquareBracket, ..] => new SelectRoot(context.Tokens[..3]),
                _ => null
            };
        }

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is [RightSquareBracket bracket, ..])
            {
                context.CurrentElement.Finish(context, [bracket]);
                context.RemoveTokensFromStart(1);
                return;
            }

            //This should contains surrounding tokens, query or whole hLinq query
            throw new InvalidTokenCollectionException(context.Tokens.GetFirstItems(5).ToArray(),
                [new RightSquareBracket(default)]);
        }

        private static SelectRoot ThrowOnEmptySelect(IParsingContext context) =>
            throw new InvalidTokenCollectionException(
                context.Tokens.GetFirstItems(3).ToArray(), [
                    new SelectToken(default), new LeftSquareBracket(default),
                    new Entity(default), new PropertyAccess(default), new RightSquareBracket(default)
                ]);
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