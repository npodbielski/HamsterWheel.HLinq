using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq;

public class HLinqQueryBinder(IHLinqParser parser, IHLinqTokenizer tokenizer, IMethodsCache methodsCache)
    : IHLinqQueryBinder
{
    public IHLinqQuery BindQuery(string queryString, Type model)
    {
        var tokens = tokenizer.Tokenize(queryString);
        var parserMethod = methodsCache.GetInstanceGeneric(parser.GetType(), nameof(parser.Parse),
            typeParams: model);
        return (IHLinqQuery?)parserMethod.Invoke(parser, [tokens, queryString]) ??
               throw new HLinqQueryParserReturnedNullException();
    }
}