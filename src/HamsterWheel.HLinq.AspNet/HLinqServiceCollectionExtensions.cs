namespace HamsterWheel.HLinq.AspNet;

public static class HLinqServiceCollectionExtensions
{
    /// <summary>
    /// Takes a service the API configures away, so that HLinq's own registration of it can be replaced
    /// with one of the API's own.
    /// </summary>
    public static void RemoveService<T>(this IServiceCollection services) where T : class
    {
        var originalImplementation = services.First(sd => sd.ServiceType == typeof(T));
        services.Remove(originalImplementation);
    }
}
