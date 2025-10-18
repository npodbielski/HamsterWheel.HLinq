using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.AspNet;

public class HLinqExtensionsConfiguration
{
    public void AddTokenPossibility<T>() where T : class, IHLinqTokenPossibility =>
        CustomServices.Add(s => s.AddSingleton<IHLinqTokenPossibility, T>());

    public void OverWriteTokenPossibility<TPossibility, TToken>() where TPossibility : class, IHLinqTokenPossibility
    {
        CustomServices.Add(s =>
        {
            var originalImplementation = s.First(sd =>
            {
                if (sd.ImplementationType?.BaseType?.IsGenericType != true)
                {
                    return false;
                }

                var baseType = sd.ImplementationType?.BaseType;
                return baseType?.GetGenericTypeDefinition() == typeof(TokenPossibility<>)
                       && baseType.GenericTypeArguments[0] == typeof(TToken);
            });
            s.Remove(originalImplementation);
            s.AddSingleton<IHLinqTokenPossibility, TPossibility>();
        });
    }

    public void RemoveService<T>(IServiceCollection s) where T : class
    {
        var originalImplementation = s.First(sd => sd.ServiceType == typeof(T));
        s.Remove(originalImplementation);
    }

    public void AddDbFunctions<T>() where T : class, IStaticMethodSource =>
        CustomServices.Add(s => s.AddSingleton<IStaticMethodSource, T>());

    public List<Action<IServiceCollection>> CustomServices { get; } = [];

    internal void InstallAll(IServiceCollection services)
    {
        foreach (var service in CustomServices)
        {
            service(services);
        }
    }
}