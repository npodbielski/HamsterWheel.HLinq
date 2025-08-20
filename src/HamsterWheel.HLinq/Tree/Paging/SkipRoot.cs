using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class SkipRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public string GetSkipNumber(string query)
    {
        return (GetChildOfType<SkipOrTakeConstant>() ??
                throw new InvalidOperationException(
                    "skip query method needs to have number parameter"))
            .Value.GetValue(query);
    }

    public sealed class Parser : ElementParserBase<SkipRoot>
    {
        protected override SkipRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [Skip, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [Skip, LeftSquareBracket, ..] => new SkipRoot(context.Tokens[..2]),
                [Dot, Skip, LeftSquareBracket, ..] => new SkipRoot(context.Tokens[..3]),
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

        private static SkipRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(
                context.Tokens.GetFirstItems(3).ToArray(), [
                    new Skip(default), new LeftSquareBracket(default), new NameOrValue(default), new RightSquareBracket(default)
                ]);
    }

    public sealed class Applier(IValueConverterFactory factory, IMethodsCache methodsCache) : RootApplierBase<SkipRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, SkipRoot skip,
            string hLinqQuery)
        {
            var type = typeof(int);
            var converter = factory.GetConverterFor(type);

            var numberAsString = skip.GetSkipNumber(hLinqQuery);

            var skipNumber = (int)converter.Convert(numberAsString)!;

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Skip),
                typeParams: context.CurrentResultType);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, skipNumber])!,
                context.CurrentResultType, context.Count);
        }
    }
}