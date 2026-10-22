using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.Sqlite;

/// <summary>
/// Provides Db functions specific to the SQLite engine to be used in HLinq queries
/// </summary>
public class SqliteEntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } = [typeof(DbFunctionsExtensions)];
}