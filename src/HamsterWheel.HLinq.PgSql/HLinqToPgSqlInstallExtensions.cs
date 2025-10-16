using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.PgSql;

public static class HLinqToPgSqlInstallExtensions
{
    public static HLinqConfiguration AddHLingToPgSql(this HLinqConfiguration configuration)
    {
        configuration.Extensions.AddDbFunctions<EntityFrameworkStaticMethodProvider>();
        return configuration;
    }
}