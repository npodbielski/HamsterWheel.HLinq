using HamsterWheel.HLinq.AspNet.Binder;
using HamsterWheel.HLinq.AspNet.ExceptionHandlers;

namespace HamsterWheel.HLinq.AspNet;

public static class AspNetInstaller
{
    public static IServiceCollection ConfigureHLinq(this IServiceCollection serviceCollection,
        Action<HLinqServicesConfiguration>? configure = null)
    {
        var configuration = new HLinqServicesConfiguration();
        configure?.Invoke(configuration);
        HLinqCore.ConfigureServices(serviceCollection, configuration.ApiServices);
        foreach (var extension in configuration.Extensions)
        {
            extension(serviceCollection);
        }

        serviceCollection.AddSingleton<IHLinqCore, HLinqCore>();
        serviceCollection.AddSingleton<WebHLinqQueryBinder>();
        serviceCollection.AddSingleton<IHLinqQueryBinder>(c => c.GetRequiredService<WebHLinqQueryBinder>());
        serviceCollection.AddExceptionHandler<HLinqQueryExceptionHandler>();
        serviceCollection.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new HLinqQueryBinderProvider());
        });

        return serviceCollection;
    }
}