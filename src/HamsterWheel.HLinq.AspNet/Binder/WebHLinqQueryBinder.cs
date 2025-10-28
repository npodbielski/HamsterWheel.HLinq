using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HamsterWheel.HLinq.AspNet.Binder;

/// <summary>
/// Used from Controllers of ASP.NET application to bind HLinqQuery
/// </summary>
/// <param name="parser"></param>
/// <param name="tokenizer"></param>
/// <param name="methodsCache"></param>
public class WebHLinqQueryBinder(HLinqBinderDependenciesBag dependenciesBag) : HLinqQueryBinder(dependenciesBag), IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var httpQueryString = bindingContext.HttpContext.Request.QueryString;
        var queryString = httpQueryString.ToString();
        var query = BindQuery(queryString, bindingContext.ModelType);

        bindingContext.Result = ModelBindingResult.Success(query);

        return Task.CompletedTask;
    }
}