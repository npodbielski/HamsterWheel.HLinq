using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.PgSql;

public class PgSqlEntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } = [typeof(NpgsqlDbFunctionsExtensions)];
}