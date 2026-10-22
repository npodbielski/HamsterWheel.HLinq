using HamsterWheel.HLinq.AspNet.Binder;
using HamsterWheel.HLinq.AspNet.ExceptionHandlers;
using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.AspNet;

public static class AspNetInstaller
{
    public static IServiceCollection ConfigureHLinq(this IServiceCollection serviceCollection,
        Action<HLinqConfiguration>? configure = null)
    {
        var configuration = new HLinqConfiguration();
        configure?.Invoke(configuration);

        serviceCollection.AddExceptionHandler<HLinqQueryExceptionHandler>();
        var hlinqOptions = new HLinqOptions
        {
            HttpDefaultMaxTakeRecords = configuration.HLinqOptions.HttpDefaultMaxTakeRecords,
            EmitWarnings = configuration.HLinqOptions.EmitWarnings
        };

        serviceCollection.AddSingleton<IHLinqOptions>(hlinqOptions);
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddScoped<IHLinqWarnings, HttpResponseWarnings>();
        serviceCollection.AddControllers(options =>
            options.ModelBinderProviders.Insert(0, new HLinqQueryBinderProvider()));

        HLinqCore.ConfigureServices(serviceCollection, hlinqOptions);

        configuration.Extensions.InstallAll(serviceCollection);

        serviceCollection.AddSingleton<IStartupFilter>(s => new HLinqGrammarFilter(s));

        return serviceCollection;
    }
}