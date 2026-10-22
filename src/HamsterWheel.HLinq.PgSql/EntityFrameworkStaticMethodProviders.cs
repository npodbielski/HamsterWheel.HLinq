using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.PgSql;

/// <summary>
/// Provides Db functions specific to the Postgres engine to be used in HLinq queries
/// </summary>
public class PgSqlEntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } =
    [
        typeof(NpgsqlDbFunctionsExtensions),
        typeof(NpgsqlTrigramsDbFunctionsExtensions)
    ];
}
