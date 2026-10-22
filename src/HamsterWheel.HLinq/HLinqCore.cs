using System.Reflection;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Filtering;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq;

internal static class HLinqCore
{
    private static TypeInfo[] GetTypesFromAssemblyWithStatic<T>() =>
    [
        .. typeof(HLinqCore).Assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false } &&
                        t.ImplementedInterfaces.Contains(typeof(T)))
    ];

    public static void ConfigureServices(IServiceCollection services, HLinqOptions options)
    {
        services.AddSingleton<IHLinqOptions>(options);

        services.AddSingleton<IGrammar, Grammar>();
        foreach (var tp in GetTypesFromAssemblyWithStatic<IHLinqWord>())
        {
            services.AddSingleton(typeof(IHLinqWord), tp);
        }

        services.AddSingleton<INullKeyword, NullKeyword>();
        services.AddSingleton<ICultureParameterKeyword, CultureParameterKeyword>();
        services.AddSingleton<IHLinqQueryParameters, HLinqQueryParameters>();

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
        foreach (var vc in GetTypesFromAssemblyWithStatic<IConfigurableValueConverter>())
        {
            services.AddSingleton(typeof(IConfigurableValueConverter), c => c.GetRequiredService(vc));
            services.AddSingleton(vc, vc);
        }

        //factories
        services.AddSingleton<IConverterFactory, ConverterFactory>();

        foreach (var c in GetTypesFromAssemblyWithStatic<IElementApplier>())
        {
            services.AddSingleton(typeof(IElementApplier), c);
        }

        services.AddSingleton<IElementApplierFactory, ApplierFactory>();
        services.AddSingleton<IHLinqQueryApplier, HLinqQueryApplier>();

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
        services.AddSingleton<IHLinqQueryBinder, HLinqQueryBinder>();
    }
}