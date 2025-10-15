using System.Collections;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq;

public static class HLinqObjectExtensions
{
    private static readonly HLinqObjectExtensionsStaticEntryPoint EntryPoint = new();

    private static readonly MethodsCache MethodsCache = new();

    private static readonly HLinqQueryBinder Binder = new(EntryPoint.DependenciesBag);

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

        var hLinqQuery = Binder.BindQuery(query, typeof(HLinqQuery<T>));

        var method = MethodsCache.GetStaticGeneric(typeof(HLinqObjectExtensions), nameof(CreateList),
            typeParams: obj.GetType());
        var list = method.Invoke(null, [obj]) as IEnumerable ??
                   throw new ExpectedImplementationOfIEnumerableException();

        var result = EntryPoint.DependenciesBag.QueryApplier.ApplyGetType(list.AsQueryable(), hLinqQuery, obj.GetType());
        return result.Data?.FirstOrDefault() ?? result.Count;
    }

    private static List<T> CreateList<T>(this T obj) => [obj];

    public class ExpectedImplementationOfIEnumerableException()
        : HLinqQueryException($"Expected object that implements: '{nameof(IEnumerable<int>)}' interface");

    internal class HLinqObjectExtensionsStaticEntryPoint
    {
        public HLinqBinderDependenciesBag DependenciesBag { get; } = CreateDefaultProvider().GetRequiredService<HLinqBinderDependenciesBag>();

        private static ServiceProvider CreateDefaultProvider()
        {
            var servicesCollection = new ServiceCollection();
            HLinqCore.ConfigureServices(servicesCollection, new HLinqOptions());
            return servicesCollection.BuildServiceProvider();
        }
    }
}