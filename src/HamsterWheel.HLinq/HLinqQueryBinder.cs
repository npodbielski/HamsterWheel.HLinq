using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Request;

namespace HamsterWheel.HLinq;

public class HLinqQueryBinder(HLinqBinderDependenciesBag dependenciesBag)
    : IHLinqQueryBinder
{
    public IHLinqQuery BindQuery(string queryString, Type model)
    {
        var parserMethod = dependenciesBag.MethodsCache.GetStaticOrThrow(model, nameof(HLinqQuery<object>.Parse), null);
        return (IHLinqQuery?)parserMethod.Invoke(null, [dependenciesBag, queryString]) ??
               throw new HLinqQueryParserReturnedNullException();
    }

    private class HLinqQueryParserReturnedNullException()
        : HLinqQueryException($"'{nameof(IHLinqParser)}' should never return null");
}