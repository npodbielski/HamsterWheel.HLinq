using System.Reflection;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.ValueConverters;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq;

internal sealed class HLinqCore(IServiceProvider? provider = null) : IHLinqCore
{
    private IElementParser[]? _coreParsers;
    private IElementToExpressionConverter[]? _coreConverters;
    private IElementToMemberAssignmentConverter[]? _coreMemberAssignmentConverters;
    private IHLinqTokenPossibility[]? _coreTokenPossibilities;

    private IServiceProvider Provider => provider ?? CreateDefault();
    private IElementParser[] CoreParsers => _coreParsers ??= GetFromAssemblyWith<TreeBranch, IElementParser>();

    private IElementToExpressionConverter[] CoreExpressionConverters =>
        _coreConverters ??= GetFromAssemblyWith<TreeBranch, IElementToExpressionConverter>();

    private IElementToMemberAssignmentConverter[] CoreMemberAssignmentConverters =>
        _coreMemberAssignmentConverters ??= GetFromAssemblyWith<TreeBranch, IElementToMemberAssignmentConverter>();

    private IHLinqTokenPossibility[] CoreTokenPossibilities =>
        _coreTokenPossibilities ??= GetFromAssemblyWith<TokenBase, IHLinqTokenPossibility>();

    public IConverterFactory ConverterFactory => Provider.GetRequiredService<IConverterFactory>();
    public IHLinqQueryApplier QueryApplier => Provider.GetRequiredService<IHLinqQueryApplier>();
    public IHLinqOptions Options => Provider.GetRequiredService<IHLinqOptions>();
    public IHLinqParser HLinqParser => Provider.GetRequiredService<IHLinqParser>();
    public IHLinqTokenizer Tokenizer => Provider.GetRequiredService<IHLinqTokenizer>();
    public IMethodsCache MethodsCache => Provider.GetRequiredService<IMethodsCache>();

    public IElementParser[] Parsers => CoreParsers;
    public IElementToExpressionConverter[] ExpressionConverters => CoreExpressionConverters;
    public IElementToMemberAssignmentConverter[] AssignmentConverters => CoreMemberAssignmentConverters;
    public IHLinqTokenPossibility[] TokenPossibilities => CoreTokenPossibilities;

    public T[] GetFromAssemblyWith<TSource, T>() => GetFromAssemblyWithStatic<TSource, T>();

    public static TypeInfo[] GetTypesFromAssemblyWithStatic<TSource, T>() =>
        typeof(TSource).Assembly.DefinedTypes
            .Where(t => !t.IsAbstract && t.ImplementedInterfaces.Contains(typeof(T)))
            .ToArray();

    private static T[] GetFromAssemblyWithStatic<TSource, T>() =>
        //TODO: probably each activator should be wrapped in try, catch to make this code resilient
        GetTypesFromAssemblyWithStatic<TSource, T>().Select(Activator.CreateInstance).Cast<T>().ToArray();

    private static ServiceProvider CreateDefault()
    {
        var servicesCollection = new ServiceCollection();
        ConfigureServices(servicesCollection);

        return servicesCollection.BuildServiceProvider();
    }

    public static void ConfigureServices(IServiceCollection servicesCollection,
        IServiceProvider? apiServicesCollection = null)
    {
        servicesCollection.AddSingleton<IHLinqCore, HLinqCore>();

        var valueConverters = GetTypesFromAssemblyWithStatic<HLinqCore, IValueConverter>();
        foreach (var vc in valueConverters.Where(t => t != typeof(ConfigurableToPlainValueConverter)))
        {
            servicesCollection.AddSingleton(typeof(IValueConverter), c => c.GetRequiredService(vc));
            servicesCollection.AddSingleton(vc, vc);
        }

        var configurableValueConverters = GetTypesFromAssemblyWithStatic<HLinqCore, IConfigurableValueConverter>();
        foreach (var vc in configurableValueConverters)
        {
            servicesCollection.AddSingleton(typeof(IConfigurableValueConverter), c => c.GetRequiredService(vc));
            servicesCollection.AddSingleton(vc, vc);
        }

        servicesCollection.AddSingleton<IValueConverterFactory, ValueConverterFactory>();

        var converters = GetTypesFromAssemblyWithStatic<HLinqCore, IElementToExpressionConverter>();
        foreach (var c in converters)
        {
            servicesCollection.AddSingleton(typeof(IElementToExpressionConverter), c);
        }

        var assignmentConverters = GetTypesFromAssemblyWithStatic<HLinqCore, IElementToMemberAssignmentConverter>();
        foreach (var c in assignmentConverters)
        {
            servicesCollection.AddSingleton(typeof(IElementToMemberAssignmentConverter), c);
        }

        servicesCollection.AddSingleton<IConverterFactory, ConverterFactory>();

        var appliers = GetTypesFromAssemblyWithStatic<HLinqCore, IApplier>();
        foreach (var c in appliers)
        {
            servicesCollection.AddSingleton(typeof(IApplier), c);
        }

        servicesCollection.AddSingleton<IApplierFactory, ApplierFactory>();
        servicesCollection.AddSingleton<IHLinqQueryApplier, HLinqQuery<object>.HLinqQueryApplier>();

        //builders
        servicesCollection.AddSingleton<IExpressionBuilder, ExpressionBuilder>();
        servicesCollection.AddSingleton<IParametersConverter, ParametersConverter>();

        //reflection helpers
        servicesCollection.AddSingleton<IPropertiesCache, PropertiesCache>();
        servicesCollection.AddSingleton<IMethodsCache, MethodsCache>();
        servicesCollection.AddSingleton<IHLinqParser, HLinqParser>();
        servicesCollection.AddSingleton<IHLinqTokenizer, HLinqTokenizer>();
        servicesCollection.AddSingleton<IHLinqParsersCollection, HLinqServicesCollection>();
        servicesCollection.AddSingleton<IDefaultConverter, DefaultConverter>();
        servicesCollection.AddSingleton<IHLinqOptions, HLinqOptions>();

        if (apiServicesCollection is not null)
        {
            servicesCollection.AddSingleton<IStaticMethodSource>(_ =>
                new StaticMethodSourceWrapper(apiServicesCollection.GetServices<IStaticMethodSource>()));
        }
    }
}