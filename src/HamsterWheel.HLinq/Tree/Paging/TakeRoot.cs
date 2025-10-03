using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class TakeRoot : TreeBranch, ITreeRoot
{
    /// <summary>
    /// If nothing in HTTP query was specified to limit number of records to fetch, it could potentially cause HLinq to query and return entire database data.
    /// To limit this default   
    /// </summary>
    private readonly int? _settingsTakeValue;

    public TakeRoot(int settingsTakeValue) : this([])
    {
        _settingsTakeValue = settingsTakeValue;
    }

    public TakeRoot(IToken[] tokens) : base(tokens)
    {
    }

    private int GetTakeNumber(string query, IValueConverter converter)
    {
        if (_settingsTakeValue is not null)
        {
            return _settingsTakeValue.Value;
        }

        var takeNumberAsString = (GetChildOfType<SkipOrTakeConstant>() ??
                                  throw new InvalidOperationException(
                                      "take query method needs to have number parameter")).Value.GetValue(query);

        return (int)converter.Convert(takeNumberAsString)!;
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
        protected override QueryableContext ApplyImpl(IQueryableContext context, TakeRoot take,
            string hLinqQuery)
        {
            var type = typeof(int);
            var converter = factory.GetConverterFor(type);

            var takeNumber = take.GetTakeNumber(hLinqQuery, converter);

            //TODO: add support of queryable.Take(0..19) which would be mych nicer to query specific range of items
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Take),
                infos => infos[1].ParameterType == typeof(int),
                context.CurrentResultType);

            return new QueryableContext((IQueryable)method.Invoke(null, [context.Queryable, takeNumber])!,
                context.CurrentResultType, context.Count);
            ;
        }
    }
}