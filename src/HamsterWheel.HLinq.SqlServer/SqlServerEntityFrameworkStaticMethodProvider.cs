using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.SqlServer;

/// <summary>
/// Provides Db functions specific to the SQL Server engine to be used in HLinq queries
/// </summary>
public class SqlServerEntityFrameworkStaticMethodProvider : IStaticMethodSource
{
    public Type[] Types { get; } = [typeof(SqlServerDbFunctionsExtensions)];
}
