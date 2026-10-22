using System.Globalization;
using System.Reflection;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using System.Collections;

namespace HamsterWheel.HLinq.Pipeline;

public class HLinqQueryApplier(IElementApplierFactory applierFactory, IMethodsCache methodsCache,
    IExpressionBuilder builder) : IHLinqQueryApplier
{
    private static readonly Lazy<HashSet<string>> KnownCultureNames = new(() =>
        new HashSet<string>(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name),
            StringComparer.OrdinalIgnoreCase));

    public object Apply<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery,
        CancellationToken cancellationToken = default) =>
        Apply(queryable, typeof(T1), hLinqQuery, cancellationToken);

    public object Apply(IQueryable queryable, Type itemType, IHLinqQuery hLinqQuery,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var result = ApplyGetType(queryable, hLinqQuery, itemType, token);
        return result.Data is null && result.Count is not null
            ? result.Count
            : result.Data ?? throw new InvalidApplierResultException();
    }

    public IResult ApplyGetType<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery,
        CancellationToken cancellationToken = default) =>
        ApplyGetType(queryable, hLinqQuery, typeof(T1), cancellationToken);

    public IResult ApplyGetType(IQueryable queryable, IHLinqQuery hLinqQuery, Type itemType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var culture = GetQueryCulture(hLinqQuery);
        IQueryableContext endResult = new QueryableContext(queryable, itemType)
        {
            Warnings = hLinqQuery.Warnings
        };
        foreach (var methodRoot in hLinqQuery.Children.OfType<ITreeBranch>())
        {
            var applier = applierFactory.Get(methodRoot);
            endResult = applier.Apply(endResult, methodRoot, hLinqQuery.SourceQueryString, culture,
                cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (endResult.Queryable is null)
        {
            return new Result(null, endResult.CurrentResultType, endResult.Count);
        }

        if (endResult.Grouping is { } grouping)
        {
            var projection = builder.GroupProjection(grouping);
            var groupSelect = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Select),
                infos => infos[1].ParameterType.IsGenericType &&
                         infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                         typeof(Func<,>), grouping.GroupingItemType, projection.ResultType);

            endResult = endResult.Next(
                (IQueryable)groupSelect.Invoke(null, [endResult.Queryable, projection.Expression])!,
                projection.ResultType);
        }

        var method = methodsCache.GetStaticGeneric(typeof(Enumerable), nameof(Enumerable.ToArray),
            typeParams: endResult.CurrentResultType);

        try
        {
            var data = (ICollection)method.Invoke(null, [endResult.Queryable])!;
            return new Result(data.Cast<object>().ToArray(), endResult.CurrentResultType);
        }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException ioe &&
                                                  ioe.Message.Contains(
                                                      "is not supported because the query has switched to client-evaluation"))
        {
            throw new DbFunctionsNotAvailableExceptions(method.Name);
        }
    }

    private static CultureInfo GetQueryCulture(IHLinqQuery hLinqQuery) =>
        hLinqQuery.Culture switch
        {
            null or "" => CultureInfo.InvariantCulture,
            var name => GetCultureInfo(name)
        };

    internal static CultureInfo GetCultureInfo(string name)
    {
        switch (KnownCultureNames.Value.Contains(name))
        {
            case false:
                throw new UnknownCultureException(name);
            default:
                try
                {
                    return CultureInfo.GetCultureInfo(name);
                }
                catch (CultureNotFoundException)
                {
                    throw new UnknownCultureException(name);
                }
        }
    }

    private class DbFunctionsNotAvailableExceptions(string name) : HLinqQueryException(
        $"Db Function `{name}` are not available in current context. Either source is in memory collection or it was evaluated prior applying HLinq");
}