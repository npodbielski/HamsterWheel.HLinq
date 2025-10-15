using System.Reflection;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.ValueConverters;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq;

internal static class HLinqCore
{
    private static TypeInfo[] GetTypesFromAssemblyWithStatic<T>() =>
        typeof(HLinqCore).Assembly.DefinedTypes
            .Where(t => !t.IsAbstract && t.ImplementedInterfaces.Contains(typeof(T)))
            .ToArray();

    public static void ConfigureServices(IServiceCollection servicesCollection, HLinqOptions options)
    {
        servicesCollection.AddSingleton<IHLinqOptions>(options);

        //tokens
        foreach (var tp in GetTypesFromAssemblyWithStatic<IHLinqTokenPossibility>())
        {
            servicesCollection.AddSingleton(typeof(IHLinqTokenPossibility), tp);
        }

        //parsers
        foreach (var ep in GetTypesFromAssemblyWithStatic<IElementParser>())
        {
            servicesCollection.AddSingleton(typeof(IElementParser), ep);
        }

        //converters
        foreach (var tec in GetTypesFromAssemblyWithStatic<IElementToExpressionConverter>())
        {
            servicesCollection.AddSingleton(typeof(IElementToExpressionConverter), tec);
        }

        foreach (var mac in GetTypesFromAssemblyWithStatic<IElementToMemberAssignmentConverter>())
        {
            servicesCollection.AddSingleton(typeof(IElementToMemberAssignmentConverter), mac);
        }

        servicesCollection.AddSingleton<IPropertyMethodToExpressionConverter, PropertyMethodToExpressionConverter>();
        servicesCollection.AddSingleton<IStaticMethodToExpressionConverter, StaticMethodToExpressionConverter>();

        //value converters
        var valueConverters = GetTypesFromAssemblyWithStatic<IValueConverter>();
        foreach (var vc in valueConverters.Where(t => t != typeof(ConfigurableToPlainValueConverter)))
        {
            servicesCollection.AddSingleton(typeof(IValueConverter), c => c.GetRequiredService(vc));
            servicesCollection.AddSingleton(vc, vc);
        }

        foreach (var vc in GetTypesFromAssemblyWithStatic<IConfigurableValueConverter>())
        {
            servicesCollection.AddSingleton(typeof(IConfigurableValueConverter), c => c.GetRequiredService(vc));
            servicesCollection.AddSingleton(vc, vc);
        }

        //factories
        servicesCollection.AddSingleton<IValueConverterFactory, ValueConverterFactory>();
        servicesCollection.AddSingleton<IConverterFactory, ConverterFactory>();

        foreach (var c in GetTypesFromAssemblyWithStatic<IApplier>())
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
        servicesCollection.AddSingleton<IDefaultConverter, DefaultConverter>();
        servicesCollection.AddSingleton<IHLinqOptions, HLinqOptions>();
        servicesCollection.AddSingleton<HLinqBinderDependenciesBag>();
    }
}