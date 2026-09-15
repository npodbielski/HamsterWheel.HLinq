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

    public SkipOrTakeConstant? Constant => GetChildOfType<SkipOrTakeConstant>();

    public SkipOrTakeRange? Range => GetChildOfType<SkipOrTakeRange>();

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

    public sealed class Applier(IDefaultConverter converter, IMethodsCache methodsCache, IHLinqOptions options)
        : RootApplierBase<TakeRoot>
    {
        /// <summary>
        /// To make sure that a silly or malicious user will not overload the system by fetching big quantities of data limit max take to this value. If user provide bigger value then <see cref="MaxTake"/> is used instead.
        /// </summary>
        private int MaxTake => options.HttpDefaultMaxTakeRecords;

        protected override IQueryableContext ApplyImpl(IQueryableContext context, TakeRoot take, string hLinqQuery)
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

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, takeParam])!,
                context.CurrentResultType, context.Count);
        }

        /// <summary>
        ///     Applies <c>take[start..end]</c> as <c>Queryable.Skip(start)</c> followed by
        ///     <c>Queryable.Take(end - start)</c>. Open ends: <c>take[start..]</c> is a plain skip
        ///     (uncapped, like <c>skip[start]</c>), <c>take[..end]</c> is a plain take. The
        ///     <see cref="MaxTake"/> cap applies to the taken row count (<c>end - start</c>), the skip
        ///     side is never capped. Deliberately not using the .NET 10 <c>Take(Range)</c> overload -
        ///     the library multi-targets netstandard2.0 and EF providers do not translate it.
        /// </summary>
        private IQueryableContext ApplyRange(IQueryableContext context, TakeRoot take, SkipOrTakeRange range,
            string hLinqQuery, Type type)
        {
            // non numeric sides throw InvalidConstantStringToTypeConversionException here, i.e. just like take[a]
            var startString = range.GetStart(hLinqQuery);
            var endString = range.GetEnd(hLinqQuery);
            var start = startString is { } startText
                ? converter.ConvertTo<int?>(startText) ?? 0
                : 0;
            var end = endString is { } endText
                ? converter.ConvertTo<int?>(endText)
                : null;

            // do not silently clamp, i.e. take[10..2], take[3..3] or take[-5..2] is just an invalid take;
            // the order check needs an explicit start so take[..0] stays valid (like take[0])
            if (start < 0 || end < 0 || startString is not null && end is not null && start >= end)
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

            return new QueryableContext(queryable, resultType, context.Count);
        }
    }
}