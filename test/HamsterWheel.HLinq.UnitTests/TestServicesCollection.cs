using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests;

public class TestServicesCollection
{
    public IServiceProvider Provider { get; } = CreateDefault();
    private IElementParser[] CoreParsers => Provider.GetServices<IElementParser>().ToArray();
    public IElementParser[] Parsers => CoreParsers;
    public IConverterFactory ConverterFactory => Provider.GetRequiredService<IConverterFactory>();
    public HLinqBinderDependenciesBag BinderDependenciesBag => Provider.GetRequiredService<HLinqBinderDependenciesBag>();
    public IHLinqTokenPossibility[] TokenPossibilities => Provider.GetServices<IHLinqTokenPossibility>().ToArray();
    public IMethodsCache MethodsCache => Provider.GetRequiredService<IMethodsCache>();
    public IPropertiesCache PropertiesCache => Provider.GetRequiredService<IPropertiesCache>();

    private static ServiceProvider CreateDefault()
    {
        var servicesCollection = new ServiceCollection();
        HLinqCore.ConfigureServices(servicesCollection, new HLinqOptions());
        return servicesCollection.BuildServiceProvider();
    }
}