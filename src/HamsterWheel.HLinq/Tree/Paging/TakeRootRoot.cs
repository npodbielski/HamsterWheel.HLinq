using HamsterWheel.HLinq.Data.ValueConverters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed partial class TakeRoot(IToken[] tokens) : TreeBranch(tokens), ITreeRoot
{
    public TakeRoot(int maxTakeValueFromSettings) : this([]) => _maxTakeValueFromSettings = maxTakeValueFromSettings;

    /// <summary>
    /// If nothing in HTTP query was specified to limit number of records to fetch, it could potentially cause HLinq to query and return entire database data.
    /// To limit this default   
    /// </summary>
    private readonly int? _maxTakeValueFromSettings;

    /// <summary>
    /// To make sure that silly or malicious user will not overload the system by fetching big quantities of data limit max take to this value. If user provide bigger value then <see cref="MaxTake"/> is used instead.
    /// </summary>
    public int MaxTake { get; set; }

    private int GetTakeNumber(string query, IValueConverter converter)
    {
        if (_maxTakeValueFromSettings is not null)
        {
            return _maxTakeValueFromSettings.Value;
        }

        var takeNumberAsString = (GetChildOfType<SkipOrTakeConstant>() ??
                                  throw new SkipOrTakeConstantTokenMissingException()).Value.GetValue(query);

        var takeNumber = (int)converter.Convert(takeNumberAsString)!;
        return (MaxTake > 0 && takeNumber > MaxTake) ? MaxTake : takeNumber;
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

    public sealed class Applier(IValueConverterFactory factory, IMethodsCache methodsCache) : RootApplierBase<TakeRoot>
    {
        protected override QueryableContext ApplyImpl(IQueryableContext context, TakeRoot take,
            string hLinqQuery)
        {
            var type = typeof(int);
            var converter = factory.GetConverterFor(type);

            var takeNumber = take.GetTakeNumber(hLinqQuery, converter);

            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Take),
                infos => infos[1].ParameterType == type,
                context.CurrentResultType);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, takeNumber])!,
                context.CurrentResultType, context.Count);
        }
    }
}