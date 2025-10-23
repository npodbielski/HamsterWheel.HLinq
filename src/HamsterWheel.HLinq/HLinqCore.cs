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

    public static void ConfigureServices(IServiceCollection services, HLinqOptions options)
    {
        services.AddSingleton<IHLinqOptions>(options);

        //tokens
        services.AddSingleton<IGrammar, Grammar>();
        foreach (var tp in GetTypesFromAssemblyWithStatic<IHLinqTokenPossibility>())
        {
            services.AddSingleton(typeof(IHLinqTokenPossibility), tp);
        }

        //parsers
        foreach (var ep in GetTypesFromAssemblyWithStatic<IElementParser>())
        {
            services.AddSingleton(typeof(IElementParser), ep);
        }

        //converters
        foreach (var tec in GetTypesFromAssemblyWithStatic<IElementToExpressionConverter>())
        {
            services.AddSingleton(typeof(IElementToExpressionConverter), tec);
        }

        foreach (var mac in GetTypesFromAssemblyWithStatic<IElementToMemberAssignmentConverter>())
        {
            services.AddSingleton(typeof(IElementToMemberAssignmentConverter), mac);
        }

        services.AddSingleton<IPropertyMethodToExpressionConverter, PropertyMethodToExpressionConverter>();
        services.AddSingleton<IStaticMethodToExpressionConverter, StaticMethodToExpressionConverter>();

        //value converters
        var valueConverters = GetTypesFromAssemblyWithStatic<IValueConverter>();
        foreach (var vc in valueConverters.Where(t => t != typeof(ConfigurableToPlainValueConverter)))
        {
            services.AddSingleton(typeof(IValueConverter), c => c.GetRequiredService(vc));
            services.AddSingleton(vc, vc);
        }

        foreach (var vc in GetTypesFromAssemblyWithStatic<IConfigurableValueConverter>())
        {
            services.AddSingleton(typeof(IConfigurableValueConverter), c => c.GetRequiredService(vc));
            services.AddSingleton(vc, vc);
        }

        //factories
        services.AddSingleton<IValueConverterFactory, ValueConverterFactory>();
        services.AddSingleton<IConverterFactory, ConverterFactory>();

        foreach (var c in GetTypesFromAssemblyWithStatic<IApplier>())
        {
            services.AddSingleton(typeof(IApplier), c);
        }

        services.AddSingleton<IApplierFactory, ApplierFactory>();
        services.AddSingleton<IHLinqQueryApplier, HLinqQuery<object>.HLinqQueryApplier>();

        //builders
        services.AddSingleton<IExpressionBuilder, ExpressionBuilder>();
        services.AddSingleton<IParametersConverter, ParametersConverter>();

        //reflection helpers
        services.AddSingleton<IPropertiesCache, PropertiesCache>();
        services.AddSingleton<IMethodsCache, MethodsCache>();
        services.AddSingleton<IHLinqParser, HLinqParser>();
        services.AddSingleton<IHLinqTokenizer, HLinqTokenizer>();
        services.AddSingleton<IDefaultConverter, DefaultConverter>();
        services.AddSingleton<IHLinqOptions, HLinqOptions>();
        services.AddSingleton<HLinqBinderDependenciesBag>();
    }
}