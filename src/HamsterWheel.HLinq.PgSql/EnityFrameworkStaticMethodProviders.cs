using HamsterWheel.HLinq.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.PgSql;

public class EntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } = [typeof(NpgsqlDbFunctionsExtensions)];
}