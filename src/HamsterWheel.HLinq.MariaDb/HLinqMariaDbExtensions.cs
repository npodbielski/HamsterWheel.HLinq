using HamsterWheel.HLinq.AspNet;

namespace HamsterWheel.HLinq.MariaDb;

public static class HLinqMariaDbExtensions
{
    public static HLinqConfiguration AddMariaDbToHLinq(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<MariaDbEntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}