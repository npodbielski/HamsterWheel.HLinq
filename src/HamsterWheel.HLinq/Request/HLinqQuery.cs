using System.Collections;
using System.Reflection;
using System.Web;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree.Paging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Request;

public partial class HLinqQuery<T> : IHLinqQuery where T : class
{
    public ITreeElement[] Children { get; private set; } = [];
    public string SourceQueryString { get; init; } = null!;
    public bool IsLeaf => false;
    public bool NoChildren => Children.Length == 0;

    /// <summary>
    /// Instance of current HttpContext <see cref="IHLinqQueryApplier"/> if <see cref="HLinqQuery{T}"/> was obtained from the binder. Otherwise, null.
    /// </summary>
    internal IHLinqQueryApplier? QueryApplier { get; set; }

    internal IHLinqOptions? Options { get; set; }

    IEnumerable<T1> ITreeElement.GetAll<T1>() => Children.OfType<T1>();
    bool ITreeElement.Finished => _finished;

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

        _finished = true;
        Children = context.Current.Children.ToArray();
    }

    private bool _finished;

    public object? ApplyTo(IQueryable<T> queryable, CancellationToken token = default)
    {
        if (QueryApplier is null)
        {
            throw new HLinqQueryQueryApplierNullException();
        }

        if (!Children.Any(t => t is TakeRoot or CountRoot) && Options?.HttpDefaultMaxTakeRecords is not null)
        {
            Children = [..Children, new TakeRoot(Options.HttpDefaultMaxTakeRecords)];
        }

        if (Children.All(t => t is not CountRoot) && Children.LastOrDefault() is TakeRoot take)
        {
            take.MaxTake = Options?.HttpDefaultMaxTakeRecords ?? HLinqOptions.DefaultMaxTakeRecords;
        }

        return QueryApplier?.Apply(queryable, this, token);
    }

    /// <summary>
    /// Used from Minimal APIs to bind Parameters of <see cref="HLinqQuery{T}"/>
    /// </summary>
    /// <returns>Instance of <see cref="HLinqQuery{T}"/> that can be applied to <see cref="IQueryable{T}"/></returns>
    public static ValueTask<HLinqQuery<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var queryString = context.Request.QueryString.Value ?? "";

        var query = Parse(context.RequestServices.GetRequiredService<IHLinqCore>(), queryString);

        return ValueTask.FromResult(query);
    }

    /// <summary>
    /// Used via Asp.Net controllers and Minimal APIs endpoint binders to parse query string into instance of <see cref="HLinqQuery{T}"/> 
    /// </summary>
    /// <param name="core">Instance of <see cref="IHLinqCore"/> from DI</param>
    /// <param name="queryString">HTTP query string</param>
    /// <returns>Instance of <see cref="HLinqQuery{T}"/></returns>
    public static HLinqQuery<T> Parse(IHLinqCore core, string queryString)
    {
        if (queryString.StartsWith('?'))
        {
            queryString = queryString[1..];
        }

        queryString = HttpUtility.UrlDecode(queryString);

        var parser = core.HLinqParser;
        var tokenizer = core.Tokenizer;
        var methodsCache = core.MethodsCache;

        var tokens = tokenizer.Tokenize(queryString);

        var parserMethod =
            methodsCache.GetInstanceGeneric(parser.GetType(), nameof(parser.Parse), typeParams: typeof(T));
        var query = (HLinqQuery<T>)parserMethod.Invoke(parser, [tokens, queryString])!;
        query.QueryApplier = core.QueryApplier;
        query.Options = core.Options;
        return query;
    }

    public class HLinqQueryApplier(IApplierFactory applierFactory, IMethodsCache methodsCache) : IHLinqQueryApplier
    {
        public object Apply<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery,
            CancellationToken cancellationToken = default) where T1 : class =>
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
            CancellationToken cancellationToken = default) where T1 : class =>
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