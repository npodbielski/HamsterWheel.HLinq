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
        var tokenPossibilities = GetTypesFromAssemblyWithStatic<IHLinqTokenPossibility>();
        foreach (var tp in tokenPossibilities)
        {
            servicesCollection.AddSingleton(typeof(IHLinqTokenPossibility), tp);
        }

        //parsers
        var elementParsers = GetTypesFromAssemblyWithStatic<IElementParser>();
        foreach (var ep in elementParsers)
        {
            servicesCollection.AddSingleton(typeof(IElementParser), ep);
        }

        //converters
        var toExpressionConverters =
            GetTypesFromAssemblyWithStatic<IElementToExpressionConverter>();
        foreach (var tec in toExpressionConverters)
        {
            servicesCollection.AddSingleton(typeof(IElementToExpressionConverter), tec);
        }

        var memberAssignmentConverters =
            GetTypesFromAssemblyWithStatic<IElementToMemberAssignmentConverter>();
        foreach (var mac in memberAssignmentConverters)
        {
            servicesCollection.AddSingleton(typeof(IElementToMemberAssignmentConverter), mac);
        }

        var valueConverters = GetTypesFromAssemblyWithStatic<IValueConverter>();
        foreach (var vc in valueConverters.Where(t => t != typeof(ConfigurableToPlainValueConverter)))
        {
            servicesCollection.AddSingleton(typeof(IValueConverter), c => c.GetRequiredService(vc));
            servicesCollection.AddSingleton(vc, vc);
        }

        var configurableValueConverters = GetTypesFromAssemblyWithStatic<IConfigurableValueConverter>();
        foreach (var vc in configurableValueConverters)
        {
            servicesCollection.AddSingleton(typeof(IConfigurableValueConverter), c => c.GetRequiredService(vc));
            servicesCollection.AddSingleton(vc, vc);
        }

        servicesCollection.AddSingleton<IValueConverterFactory, ValueConverterFactory>();
        servicesCollection.AddSingleton<IConverterFactory, ConverterFactory>();

        var appliers = GetTypesFromAssemblyWithStatic<IApplier>();
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
        servicesCollection.AddSingleton<IDefaultConverter, DefaultConverter>();
        servicesCollection.AddSingleton<IHLinqOptions, HLinqOptions>();
        servicesCollection.AddSingleton<HLinqBinderDependenciesBag>();
    }
}