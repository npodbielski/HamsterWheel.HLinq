using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class TakeRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public string GetTakeNumber(string query)
    {
        return (GetChildOfType<SkipOrTakeConstant>() ??
                throw new InvalidOperationException(
                    "take query method needs to have number parameter")).Value
            .GetValue(query);
    }

    public sealed class Parser : ElementParserBase<TakeRoot>
    {
        protected override TakeRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [Take, LeftSquareBracket, ..] => new TakeRoot(context.Tokens[..2]),
                [Dot, Take, LeftSquareBracket, ..] => new TakeRoot(context.Tokens[..3]),
                _ => default
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
    }

    public sealed class Applier(IValueConverterFactory factory, IMethodsCache methodsCache) : RootApplierBase<TakeRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, TakeRoot skip,
            string hLinqQuery)
        {
            var type = typeof(int);
            var converter = factory.GetConverterFor(type);

            var numberAsString = skip.GetTakeNumber(hLinqQuery);

            var skipNumber = (int)converter.Convert(numberAsString)!;
            var parameters = new[] { typeof(IQueryable<>).MakeGenericType(context.CurrentResultType), typeof(int) };

            //TODO: add support of queryable.Take(0..19) which would be mych nicer to query specific range of items
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Take),
                infos => infos[1].ParameterType == typeof(int),
                context.CurrentResultType);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, skipNumber])!,
                context.CurrentResultType, context.Count);
            ;
        }
    }
}