using System.Reflection;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using System.Collections;

namespace HamsterWheel.HLinq.Pipeline;

public class HLinqQueryApplier(IElementApplierFactory applierFactory, IMethodsCache methodsCache)
    : IHLinqQueryApplier
{
    public object Apply<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery,
        CancellationToken cancellationToken = default) =>
        Apply(queryable, typeof(T1), hLinqQuery, cancellationToken);

    public object Apply(IQueryable queryable, Type itemType, IHLinqQuery hLinqQuery,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var result = ApplyGetType(queryable, hLinqQuery, itemType, token);
        if (result.Data is null && result.Count is not null)
        {
            return result.Count;
        }

        return result.Data ?? throw new InvalidApplierResultException();
    }

    public IResult ApplyGetType<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery,
        CancellationToken cancellationToken = default) =>
        ApplyGetType(queryable, hLinqQuery, typeof(T1), cancellationToken);

    public IResult ApplyGetType(IQueryable queryable, IHLinqQuery hLinqQuery, Type itemType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IQueryableContext endResult = new QueryableContext(queryable, itemType);
        foreach (var methodRoot in hLinqQuery.Children.OfType<ITreeBranch>())
        {
            var applier = applierFactory.Get(methodRoot);
            endResult = applier.Apply(endResult, methodRoot, hLinqQuery.SourceQueryString, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (endResult.Queryable is null)
        {
            return new Result(null, endResult.CurrentResultType, endResult.Count);
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

    public class DbFunctionsNotAvailableExceptions(string name) : HLinqQueryException(
        $"Db Function `{name}` are not available in current context. Either source is in memory collection or it was evaluated prior applying HLinq");
}