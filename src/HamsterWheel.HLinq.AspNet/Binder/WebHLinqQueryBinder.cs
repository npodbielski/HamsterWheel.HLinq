using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HamsterWheel.HLinq.AspNet.Binder;

/// <summary>
/// Used from Controllers of ASP.NET application to bind HLinqQuery
/// </summary>
/// <param name="parser"></param>
/// <param name="tokenizer"></param>
/// <param name="methodsCache"></param>
public class WebHLinqQueryBinder(IHLinqParser parser, IHLinqTokenizer tokenizer, IMethodsCache methodsCache)
    : HLinqQueryBinder(parser, tokenizer, methodsCache), IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var httpQueryString = bindingContext.HttpContext.Request.QueryString;
        var queryString = httpQueryString.ToString();
        var genericArgument = bindingContext.ModelType.GetGenericArguments()[0];

        var query = BindQuery(queryString, genericArgument);

        bindingContext.Result = ModelBindingResult.Success(query);

        return Task.CompletedTask;
    }
}