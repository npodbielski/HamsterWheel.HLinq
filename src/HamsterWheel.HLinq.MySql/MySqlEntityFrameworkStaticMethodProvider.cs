using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.MySql;

/// <summary>
/// Provides Db functions specific to the MySQL engine to be used in HLinq queries
/// </summary>
public class MySqlEntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } =
    [
        typeof(DbFunctionsExtensions),
        typeof(MySqlDbFunctionsExtensions),
        typeof(MySqlJsonDbFunctionsExtensions)
    ];
}