using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.TestUtils;

public class TestServicesCollection
{
    public IServiceProvider Provider { get; } = CreateDefault();
    private IElementParser[] CoreParsers => [.. Provider.GetServices<IElementParser>()];
    public IElementParser[] Parsers => CoreParsers;
    public IConverterFactory ConverterFactory => Provider.GetRequiredService<IConverterFactory>();
    public HLinqBinderDependenciesBag BinderDependenciesBag => Provider.GetRequiredService<HLinqBinderDependenciesBag>();
    public IHLinqWord[] TokenWords => [.. Provider.GetServices<IHLinqWord>()];
    public IHLinqTokenizer Tokenizer => Provider.GetRequiredService<IHLinqTokenizer>();
    public IMethodsCache MethodsCache => Provider.GetRequiredService<IMethodsCache>();
    public IPropertiesCache PropertiesCache => Provider.GetRequiredService<IPropertiesCache>();

    private static ServiceProvider CreateDefault()
    {
        var servicesCollection = new ServiceCollection();
        HLinqCore.ConfigureServices(servicesCollection, new HLinqOptions());
        return servicesCollection.BuildServiceProvider();
    }
}