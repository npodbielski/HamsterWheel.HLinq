using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Request;

namespace HamsterWheel.HLinq;

public class HLinqQueryBinder(IHLinqCore core)
    : IHLinqQueryBinder
{
    public IHLinqQuery BindQuery(string queryString, Type model)
    {
        var methodsCache = core.MethodsCache;
        var parserMethod = methodsCache.GetStatic(typeof(HLinqQuery<>).MakeGenericType(model), nameof(HLinqQuery<object>.Parse), null)
            ?? throw new MissingMethodException($"Could not find HLinqQuery<{model.Name}>.Parse method");
        return (IHLinqQuery?)parserMethod.Invoke(null, [core, queryString]) ??
               throw new HLinqQueryParserReturnedNullException();
    }
}