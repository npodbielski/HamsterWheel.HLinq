using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Request;

namespace HamsterWheel.HLinq;

public class HLinqQueryBinder(IHLinqCore core)
    : IHLinqQueryBinder
{
    public IHLinqQuery BindQuery(string queryString, Type model)
    {
        var parserMethod = core.MethodsCache.GetStaticOrThrow(typeof(HLinqQuery<>).MakeGenericType(model),
            nameof(HLinqQuery<object>.Parse), null);
        return (IHLinqQuery?)parserMethod.Invoke(null, [core, queryString]) ??
               throw new HLinqQueryParserReturnedNullException();
    }

    private class HLinqQueryParserReturnedNullException()
        : HLinqQueryException($"'{nameof(IHLinqParser)}' should never return null");
}