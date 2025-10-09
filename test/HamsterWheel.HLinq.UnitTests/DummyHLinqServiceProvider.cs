using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests;

public class DummyHLinqServiceProviderFactory
{
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Parser);
        services.AddSingleton(MethodsCache);
        services.AddSingleton(Tokenizer);
        services.AddSingleton(QueryApplier);
        services.AddSingleton(Options);
        return services.BuildServiceProvider();
    }

    public IHLinqQueryApplier QueryApplier { get; } = Substitute.For<IHLinqQueryApplier>();

    public IMethodsCache MethodsCache { get; } = Substitute.For<IMethodsCache>();

    public IHLinqTokenizer Tokenizer { get; } = Substitute.For<IHLinqTokenizer>();

    public IHLinqParser Parser { get; } = Substitute.For<IHLinqParser>();
    public IHLinqOptions Options { get; } = new HLinqOptionsConfiguration();
    public HLinqBinderDependenciesBag BinderDependenciesBag => new(QueryApplier, Options, Parser, Tokenizer, MethodsCache);
}