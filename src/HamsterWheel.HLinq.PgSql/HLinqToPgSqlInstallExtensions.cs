using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.PgSql;

public static class HLinqToPgSqlInstallExtensions
{
    public static HLinqServicesConfiguration AddHLingToPgSql(this HLinqServicesConfiguration servicesConfiguration)
    {
        servicesConfiguration.Extensions.Add(s => s.AddSingleton<IStaticMethodSource, EntityFrameworkStaticMethodProvider>());
        return servicesConfiguration;
    }
}