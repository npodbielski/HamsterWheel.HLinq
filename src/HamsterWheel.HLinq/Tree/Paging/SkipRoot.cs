using HamsterWheel.HLinq.Data.ValueConverters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed partial class SkipRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    private string GetSkipNumber(string query) =>
        (GetChildOfType<SkipOrTakeConstant>() ?? throw new MissingSkipValueException()).Value.GetValue(query);

    public sealed class Parser : ElementParserBase<SkipRoot>
    {
        protected override SkipRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Skip, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [Skip, LeftSquareBracket, ..] => new SkipRoot(context.Tokens[..2]),
                [Dot, Skip, LeftSquareBracket, ..] => new SkipRoot(context.Tokens[..3]),
                _ => null
            };

        public override IToken[] ExampleTokens { get; } = SkipRootExampleTokens;

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

        private static SkipRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens, SkipRootExampleTokens);

        private static IToken[] SkipRootExampleTokens =>
        [
            Skip.Empty, LeftSquareBracket.Empty, new TokenExample("10"), RightSquareBracket.Empty
        ];
    }

    public sealed class Applier(IValueConverterFactory factory, IMethodsCache methodsCache) : RootApplierBase<SkipRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, SkipRoot skip,
            string hLinqQuery)
        {
            var converter = factory.GetConverterFor(typeof(int));

            var numberAsString = skip.GetSkipNumber(hLinqQuery);

            var skipNumber = (int)converter.Convert(numberAsString)!;

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Skip),
                typeParams: context.CurrentResultType);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, skipNumber])!,
                context.CurrentResultType, context.Count);
        }
    }
}