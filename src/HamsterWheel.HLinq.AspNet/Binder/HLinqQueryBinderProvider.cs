using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace HamsterWheel.HLinq.AspNet.Binder;

public class HLinqQueryBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType.GetInterfaces().Any(t => t == typeof(IHLinqQuery))
            ? new BinderTypeModelBinder(typeof(WebHLinqQueryBinder))
            : null;
    }
}