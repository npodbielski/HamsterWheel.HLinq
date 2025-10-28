using System.Diagnostics.CodeAnalysis;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
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

    //this is tested to work correctly so writing a test for this exception would require to break the HLinqQuery type or replace it to something else
    [ExcludeFromCodeCoverage]
    private class HLinqQueryParserReturnedNullException()
        : HLinqQueryException($"'{nameof(IHLinqParser)}' should never return null");
}