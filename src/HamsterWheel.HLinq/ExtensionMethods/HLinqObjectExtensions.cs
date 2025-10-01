using System.Collections;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq;

public static class HLinqObjectExtensions
{
    private static readonly HLinqCore Core = new();

    private static readonly MethodsCache MethodsCache = new();

    private static readonly HLinqQueryBinder Binder = new(Core);

    public static object? ExecuteHLinq<T>(this T? obj, string query) where T : class
    {
        if (obj is null)
        {
            return null;
        }

        if (!query.StartsWith("select["))
        {
            query = "select[" + query + "]";
        }

        var hLinqQuery = Binder.BindQuery(query, obj.GetType());

        var method = MethodsCache.GetStaticGeneric(typeof(HLinqObjectExtensions), nameof(CreateList),
            typeParams: obj.GetType());
        var list = method.Invoke(null, [obj]) as IEnumerable ??
                   throw new ExpectedImplementationOfIEnumerableException();

        var result = Core.QueryApplier.ApplyGetType(list.AsQueryable(), hLinqQuery, obj.GetType());
        return result.Data?.FirstOrDefault() ?? result.Count;
    }

    private static List<T> CreateList<T>(this T obj) => [obj];
}