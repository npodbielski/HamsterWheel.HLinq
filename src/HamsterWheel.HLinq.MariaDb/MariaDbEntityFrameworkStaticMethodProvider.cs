using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.MariaDb;

/// <summary>
/// Provides Db functions specific to the MariaDB engine to be used in HLinq queries
/// </summary>
public class MariaDbEntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } =
    [
        typeof(DbFunctionsExtensions),
        typeof(MySqlDbFunctionsExtensions),
        typeof(MySqlJsonDbFunctionsExtensions)
    ];
}