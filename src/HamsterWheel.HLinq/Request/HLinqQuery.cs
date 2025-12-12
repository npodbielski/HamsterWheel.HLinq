using System.Reflection;
using System.Web;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Paging;
using HamsterWheel.HLinq.Tree.Selecting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Request;

public class HLinqQuery<T> : IHLinqQuery
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
            throw new NonParsableTokenSequenceException(context.SourceQueryString, context.Tokens, [new WhereRoot.Parser(), new SelectRoot.Parser()]);
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
    public static async ValueTask<HLinqQuery<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var queryString = context.Request.QueryString.Value ?? "";
        return Parse(context.RequestServices.GetRequiredService<HLinqBinderDependenciesBag>(), queryString);
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
}