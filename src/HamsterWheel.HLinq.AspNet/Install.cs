using HamsterWheel.HLinq.AspNet.Binder;
using HamsterWheel.HLinq.AspNet.ExceptionHandlers;

namespace HamsterWheel.HLinq.AspNet;

public static class AspNetInstaller
{
    public static IServiceCollection ConfigureHLinq(this IServiceCollection serviceCollection,
        Action<HLinqConfiguration>? configure = null)
    {
        var configuration = new HLinqConfiguration();
        configure?.Invoke(configuration);

        serviceCollection.AddExceptionHandler<HLinqQueryExceptionHandler>();
        serviceCollection.AddSingleton<IHLinqOptions>(new HLinqOptions
        {
            HttpDefaultMaxTakeRecords = configuration.HLinqOptions.HttpDefaultMaxTakeRecords
        });
        serviceCollection.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new HLinqQueryBinderProvider());
        });

        HLinqCore.ConfigureServices(serviceCollection, new HLinqOptions
        {
            HttpDefaultMaxTakeRecords = configuration.HLinqOptions.HttpDefaultMaxTakeRecords
        });

        configuration.Extensions.InstallAll(serviceCollection);

        return serviceCollection;
    }
}