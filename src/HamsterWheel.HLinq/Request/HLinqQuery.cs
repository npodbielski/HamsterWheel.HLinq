using System.Collections;
using System.Reflection;
using System.Web;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree.Paging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Request;

public partial class HLinqQuery<T> : IHLinqQuery
{
    public ITreeElement[] Children { get; private set; } = [];
    public Type ItemType { get; } = typeof(T);
    public string SourceQueryString { get; internal init; } = null!;
    public bool NoChildren => Children.Length == 0;

    /// <summary>
    /// Instance of current <see cref="HttpContext"/>
    /// <see cref="IHLinqQueryApplier"/> if <see cref="HLinqQuery{T}"/> was obtained from the binder. Otherwise, null.
    /// </summary>
    private IHLinqQueryApplier? QueryApplier { get; set; }

    internal IHLinqOptions? Options { get; set; }

    /// <summary>
    ///     If query is valid this should always be empty. If not then we should have an error in <see cref="Finish" />
    /// </summary>
    IToken[] ITreeElement.Tokens => [];

    void ITreeBranch.Finish(IParsingContext context, IToken[] _)
    {
        if (context.Tokens.Length > 0)
        {
            throw new NonParsableTokenSequenceException(context.SourceQueryString, context.Tokens, []);
        }

        Children = context.Current.Children.ToArray();
    }

    public object? ApplyTo(IQueryable<T> queryable, CancellationToken token = default)
    {
        if (QueryApplier is null)
        {
            throw new HLinqQueryQueryApplierNullException();
        }

        if (Children.All(t => t is not CountRoot) && Children.LastOrDefault() is not TakeRoot)
        {
            Children = [..Children, new TakeRoot()];
        }

        return QueryApplier?.Apply(queryable, this, token);
    }

    /// <summary>
    /// Used from Minimal APIs to bind Parameters of <see cref="HLinqQuery{T}"/>
    /// </summary>
    /// <returns>Instance of <see cref="HLinqQuery{T}"/> that can be applied to <see cref="IQueryable{T}"/></returns>
    // ReSharper disable once UnusedMember.Global - used by Minimal APIs
    // ReSharper disable once UnusedParameter.Global - it is necessary by the binder
    public static ValueTask<HLinqQuery<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var queryString = context.Request.QueryString.Value ?? "";
        var query = Parse(context.RequestServices.GetRequiredService<HLinqBinderDependenciesBag>(), queryString);
        return ValueTask.FromResult(query);
    }

    /// <summary>
    /// Used via Asp.Net controllers and Minimal APIs endpoint binders to parse query string into instance of <see cref="HLinqQuery{T}"/> 
    /// </summary>
    /// <param name="dependenciesBag">Instance of <see cref="HLinqBinderDependenciesBag"/> from DI</param>
    /// <param name="queryString">HTTP query string</param>
    /// <returns>Instance of <see cref="HLinqQuery{T}"/></returns>
    public static HLinqQuery<T> Parse(HLinqBinderDependenciesBag dependenciesBag, string queryString)
    {
        queryString = HttpUtility.UrlDecode(queryString);
        if (queryString.StartsWith('?'))
        {
            queryString = queryString[1..];
        }

        var hlinqQuery = new HLinqQuery<T>
        {
            SourceQueryString = queryString,
        };
        var parser = dependenciesBag.HLinqParser;
        var tokenizer = dependenciesBag.Tokenizer;

        var tokens = tokenizer.Tokenize(queryString);

        parser.Parse(hlinqQuery, tokens);
        hlinqQuery.QueryApplier = dependenciesBag.QueryApplier;
        hlinqQuery.Options = dependenciesBag.Options;
        return hlinqQuery;
    }

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
    }
}