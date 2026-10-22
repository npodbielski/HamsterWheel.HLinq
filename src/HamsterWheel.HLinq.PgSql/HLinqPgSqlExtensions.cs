using HamsterWheel.HLinq.AspNet;

namespace HamsterWheel.HLinq.PgSql;

public static class HLinqPgSqlExtensions
{
    public static HLinqConfiguration AddPgSqlToHLinq(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<PgSqlEntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}