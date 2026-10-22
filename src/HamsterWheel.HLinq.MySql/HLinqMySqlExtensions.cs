using HamsterWheel.HLinq.AspNet;

namespace HamsterWheel.HLinq.MySql;

public static class HLinqMySqlExtensions
{
    public static HLinqConfiguration AddMySqlToHLinq(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<MySqlEntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}