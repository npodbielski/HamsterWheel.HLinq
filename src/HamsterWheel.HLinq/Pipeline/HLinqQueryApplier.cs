using System.Globalization;
using System.Reflection;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using System.Collections;

namespace HamsterWheel.HLinq.Pipeline;

public class HLinqQueryApplier(IElementApplierFactory applierFactory, IMethodsCache methodsCache,
    IExpressionBuilder builder, IHLinqOptions options) : IHLinqQueryApplier
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
        ValidateNumberOfProperties(hLinqQuery);
        var diagnostics = hLinqQuery.Diagnostics;
        var endResult = Process(new QueryableContext(queryable, itemType)
        {
            Diagnostics = diagnostics
        }, hLinqQuery, diagnostics, cancellationToken);

        if (diagnostics.Exception is { } endOfTheLineExc)
        {
            endOfTheLineExc.Errors = diagnostics.Errors;
            throw endOfTheLineExc;
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
            return new Result([.. data.Cast<object>()], endResult.CurrentResultType);
        }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException ioe &&
                                                  ioe.Message.Contains(
                                                      "is not supported because the query has switched to client-evaluation"))
        {
            throw new DbFunctionsNotAvailableExceptions(method.Name);
        }
    }

    public IReadOnlyList<HLinqError> Validate(IHLinqQuery hLinqQuery, Type itemType,
        IHLinqDiagnostics diagnostics)
    {
        try
        {
            ValidateNumberOfProperties(hLinqQuery);
        }
        catch (HLinqQueryException exception)
        {
            diagnostics.AddException(exception);
            return diagnostics.Errors;
        }

        var start = new QueryableContext(null, itemType)
        {
            Diagnostics = diagnostics,
            ValidateMode = true
        };

        Process(start, hLinqQuery, diagnostics, CancellationToken.None);

        return diagnostics.Errors;
    }

    /// <summary>
    /// Checks if query does not violate max number of properties limit.
    /// </summary>
    private void ValidateNumberOfProperties(IHLinqQuery hLinqQuery)
    {
        foreach (var methodRoot in hLinqQuery.Children.OfType<ITreeBranch>())
        {
            var widest = methodRoot.MaxNumberOfProperties();
            if (widest > options.MaximumNumberOfProperties)
            {
                throw new TooManyPropertiesException(widest, options.MaximumNumberOfProperties);
            }
        }
    }

    /// <summary>
    /// Processes hlinq query.
    /// </summary>
    private IQueryableContext Process(IQueryableContext context, IHLinqQuery hLinqQuery,
        IHLinqDiagnostics diagnostics, CancellationToken cancellationToken)
    {
        var endResult = context;

        CultureInfo culture;
        try
        {
            culture = GetQueryCulture(hLinqQuery);
        }
        catch (HLinqQueryException exception)
        {
            diagnostics.AddException(exception);
            return context;
        }

        foreach (var methodRoot in hLinqQuery.Children.OfType<ITreeBranch>())
        {
            diagnostics.RootKeyword = methodRoot.GetTokenKeyword();

            try
            {
                var applier = applierFactory.Get(methodRoot);
                endResult = applier.Apply(endResult, methodRoot, hLinqQuery.SourceQueryString, culture,
                    cancellationToken);
            }
            catch (HLinqQueryException exception)
            {
                diagnostics.AddException(exception);
                if (methodRoot.ChangesResponse())
                {
                    break;
                }
            }
            catch (Exception)
            {
                if (diagnostics.Exception is null)
                {
                    //HLinq bug or API exception
                    throw;
                }

                //query is already broken or invalid
                break;
            }
        }

        return endResult;
    }

    private static CultureInfo GetQueryCulture(IHLinqQuery hLinqQuery) =>
        hLinqQuery.Culture switch
        {
            null or "" => CultureInfo.InvariantCulture,
            var name => GetCultureInfo(name)
        };

    private static CultureInfo GetCultureInfo(string name)
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
