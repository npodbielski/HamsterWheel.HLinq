using HamsterWheel.HLinq.AspNet;

namespace HamsterWheel.HLinq.PgSql;

public static class HLinqToPgSqlInstallExtensions
{
    public static HLinqConfiguration AddHLingToPgSql(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<PgSqlEntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}