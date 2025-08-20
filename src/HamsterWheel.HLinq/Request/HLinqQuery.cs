using System.Collections;
using System.Reflection;
using System.Web;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Request;

public class HLinqQuery<T> : IHLinqQuery
{
    private ITreeBranch ThisTree => this;
    public string SourceQueryString { get; init; } = null!;

    public bool IsLeaf => false;

    /// <summary>
    ///     If query is valid this should always be null. If not then we should have an error in <see cref="Finish()" />
    /// </summary>
    IToken[] ITreeElement.Tokens => [];

    bool ITreeElement.IsBranch => true;

    bool ITreeElement.Finished => _finished;
    public bool NoChildren => _children.Length == 0;

    void ITreeElement.Finish(IParsingContext context, IToken[] _)
    {
        if (context.Tokens.Length > 0)
            //TODO: format better message
            throw new NonParsableTokenSequenceException(context.Tokens, []);

        _finished = true;
        _children = context.Current.Children.ToArray();
    }

    IEnumerable<T1> ITreeElement.GetAll<T1>() => ThisTree.Children.OfType<T1>();

    public ITreeElement[] Children => _children;
    private ITreeElement[] _children = [];
    private bool _finished;

    ///Used from Minimap APIs to bind Parameters of HLinqQuery
    public static ValueTask<HLinqQuery<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var queryString = context.Request.QueryString.Value ?? "";

        if (queryString.StartsWith('?'))
        {
            queryString = queryString[1..];
        }

        queryString = HttpUtility.UrlDecode(queryString);

        var query = Parse(context.RequestServices, queryString);

        return ValueTask.FromResult(query);
    }

    private static HLinqQuery<T> Parse(IServiceProvider services, string queryString)
    {
        var core = services.GetRequiredService<IHLinqCore>();
        var parser = core.HLinqParser;
        var tokenizer = core.Tokenizer;
        var methodsCache = core.MethodsCache;

        var tokens = tokenizer.Tokenize(queryString);

        var parserMethod =
            methodsCache.GetInstanceGeneric(parser.GetType(), nameof(parser.Parse), typeParams: typeof(T));
        var query = parserMethod.Invoke(parser, [tokens, queryString]);
        return (HLinqQuery<T>)query!;
    }

    public class QueryApplier(IApplierFactory applierFactory, IMethodsCache methodsCache) : IHLinqQueryApplier
    {
        public object Apply<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery,
            CancellationToken cancellationToken = default) where T1 : class
        {
            return Apply(queryable, typeof(T1), hLinqQuery, cancellationToken);
        }

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
            CancellationToken cancellationToken = default) where T1 : class
        {
            return ApplyGetType(queryable, hLinqQuery, typeof(T1), cancellationToken);
        }

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
            if (endResult.Queryable is not null)
            {
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

            return new Result(null, endResult.CurrentResultType, endResult.Count);
        }
    }
}