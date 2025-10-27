using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed partial class TakeRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public TakeRoot() : this([])
    {
    }

    private string GetTakeNumber(string query)
    {
        var takeNumberAsString = GetChildOfType<SkipOrTakeConstant>();
        return takeNumberAsString is not null ? takeNumberAsString.Value.GetValue(query) : "";
    }

    public sealed class Parser : ElementParserBase<TakeRoot>
    {
        public override IToken[] ExampleTokens => TakeRootExampleTokens;

        private static IToken[] TakeRootExampleTokens { get; } =
            [Take.Empty, LeftSquareBracket.Empty, new TokenExample("10"), RightSquareBracket.Empty];

        protected override TakeRoot? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [Take, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [Take, LeftSquareBracket, ..] => new TakeRoot(context.Tokens[..2]),
                [Dot, Take, LeftSquareBracket, ..] => new TakeRoot(context.Tokens[..3]),
                _ => default
            };
        }

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

        private static TakeRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens, TakeRootExampleTokens);
    }

    public sealed class Applier(IDefaultConverter converter, IMethodsCache methodsCache, IHLinqOptions options)
        : RootApplierBase<TakeRoot>
    {
        private int MaxTake => options.HttpDefaultMaxTakeRecords;

        protected override QueryableContext ApplyImpl(IQueryableContext context, TakeRoot take,
            string hLinqQuery)
        {
            var type = typeof(int);

            var takeString = take.GetTakeNumber(hLinqQuery);
            var takeParam = converter.ConvertTo<int?>(takeString) ?? MaxTake;
            takeParam = takeParam > MaxTake ? MaxTake : takeParam;

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Take),
                infos => infos[1].ParameterType == type,
                context.CurrentResultType);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, takeParam])!,
                context.CurrentResultType, context.Count);
        }
    }
}