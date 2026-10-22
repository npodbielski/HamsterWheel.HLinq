using HamsterWheel.HLinq.AspNet;

namespace HamsterWheel.HLinq.Sqlite;

public static class HLinqSqliteExtensions
{
    public static HLinqConfiguration AddSqliteToHLinq(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<SqliteEntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}