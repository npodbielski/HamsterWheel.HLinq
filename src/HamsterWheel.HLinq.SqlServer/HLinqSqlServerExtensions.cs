using HamsterWheel.HLinq.AspNet;

namespace HamsterWheel.HLinq.SqlServer;

public static class HLinqSqlServerExtensions
{
    /// <summary>
    /// Adds SqlServer specific DB methods that can be used in HLinq queries
    /// </summary>
    public static HLinqConfiguration AddSqlServerToHLinq(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<SqlServerEntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}
