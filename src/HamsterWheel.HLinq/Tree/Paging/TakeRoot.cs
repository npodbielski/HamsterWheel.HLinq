using System.Globalization;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class TakeRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public TakeRoot() : this([])
    {
    }

    private SkipOrTakeConstant? Constant => GetChildOfType<SkipOrTakeConstant>();

    private SkipOrTakeRange? Range => GetChildOfType<SkipOrTakeRange>();

    private string GetTakeNumber(string query)
    {
        var takeNumberAsString = Constant;
        return takeNumberAsString is not null ? takeNumberAsString.Value.GetValue(query) : "";
    }

    public sealed class Parser : ElementParserBase<TakeRoot>
    {
        public override IToken[] ExampleTokens => TakeRootExampleTokens;

        internal static IToken[] TakeRootExampleTokens { get; } =
            [Take.Empty, LeftSquareBracket.Empty, new TokenExample("10"), RightSquareBracket.Empty];

        protected override TakeRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Take, LeftSquareBracket, RightSquareBracket] => ThrowOnEmpty(context),
                [Take, LeftSquareBracket, ..] => new TakeRoot(context.Tokens[..2]),
                [Dot, Take, LeftSquareBracket, ..] => new TakeRoot(context.Tokens[..3]),
                _ => default
            };

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightSquareBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightSquareBracket.Empty]);
            }

            // rejects i.e. take[10..20..30] or take[10 20] - only one value or range is allowed per take
            if (context.Current.Children.Count(c => c is SkipOrTakeConstant or SkipOrTakeRange) > 1)
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    TakeRootExampleTokens);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }

        private static TakeRoot ThrowOnEmpty(IParsingContext context) =>
            throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens, TakeRootExampleTokens);
    }

    // ReSharper disable once UnusedType.Global - Used by Applier
    public sealed class Applier(IDefaultConverter converter, IMethodsCache methodsCache, IHLinqOptions options)
        : RootApplierBase<TakeRoot>
    {
        /// <summary>
        /// To make sure that a silly or malicious user will not overload the system by fetching big quantities of data limit max take to this value. If user provide bigger value then <see cref="MaxTake"/> is used instead.
        /// </summary>
        private int MaxTake => options.HttpDefaultMaxTakeRecords;

        protected override IQueryableContext ApplyImpl(IQueryableContext context, TakeRoot take, string hLinqQuery,
            CultureInfo culture)
        {
            var type = typeof(int);

            if (take.Range is { } range)
            {
                return ApplyRange(context, take, range, hLinqQuery, type);
            }

            var takeString = take.GetTakeNumber(hLinqQuery);
            var takeParam = converter.ConvertTo<int?>(takeString)
                            //if value is null, then it means that Take[] was added by binder to limit the number of records to return to HTTP request
                            ?? MaxTake;
            takeParam = takeParam > MaxTake ? MaxTake : takeParam;

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Take),
                infos => infos[1].ParameterType == type,
                context.CurrentResultType);

            return context.Next((IQueryable)method.Invoke(null, [context.Queryable, takeParam])!,
                context.CurrentResultType, context.Count);
        }

        private IQueryableContext ApplyRange(IQueryableContext context, TakeRoot take, SkipOrTakeRange range,
            string hLinqQuery, Type type)
        {
            // non-numeric sides throw InvalidConstantStringToTypeConversionException here, i.e. just like take[a]
            var startString = range.GetStart(hLinqQuery);
            var endString = range.GetEnd(hLinqQuery);
            var start = startString is { } startText
                ? converter.ConvertTo<int?>(startText) ?? 0
                : 0;
            var end = endString is { } endText
                ? converter.ConvertTo<int?>(endText)
                : null;

            if (start < 0 || end < 0 || startString is not null && start >= end)
            {
                throw new InvalidTokenCollectionException(hLinqQuery, take.Tokens, Parser.TakeRootExampleTokens);
            }

            var queryable = context.Queryable;
            var resultType = context.CurrentResultType;

            if (start > 0)
            {
                var skipMethod = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Skip),
                    typeParams: resultType);
                queryable = (IQueryable)skipMethod.Invoke(null, [queryable, start])!;
            }

            if (end is { } endValue)
            {
                var takeParam = endValue - start;
                takeParam = takeParam > MaxTake ? MaxTake : takeParam;

                var takeMethod = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Take),
                    infos => infos[1].ParameterType == type,
                    resultType);
                queryable = (IQueryable)takeMethod.Invoke(null, [queryable, takeParam])!;
            }

            return context.Next(queryable, resultType, context.Count);
        }
    }
}