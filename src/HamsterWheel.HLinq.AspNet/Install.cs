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

        serviceCollection.AddExceptionHandler<HLinqQueryExceptionHandler>();
        serviceCollection.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new HLinqQueryBinderProvider());
        });

        HLinqCore.ConfigureServices(serviceCollection, new HLinqOptions
        {
            HttpDefaultMaxTakeRecords = configuration.HLinqOptions.HttpDefaultMaxTakeRecords
        });

        foreach (var extension in configuration.Extensions)
        {
            extension(serviceCollection);
        }

        return serviceCollection;
    }
}