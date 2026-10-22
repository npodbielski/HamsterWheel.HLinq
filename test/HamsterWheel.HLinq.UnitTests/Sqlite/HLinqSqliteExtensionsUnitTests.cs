using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Sqlite;

public class HLinqSqliteExtensionsUnitTests
{
    [Fact]
    public void AddSqliteToHLinq_WhenCalled_ThenRegistersCorrectServices()
    {
        //arrange
        var services = new ServiceCollection();
        var configuration = new HLinqConfiguration();

        //act
        var actual = configuration.AddSqliteToHLinq();
        configuration.Extensions.CustomServices.ForEach(c => c(services));

        //assert
        actual.Should().Be(configuration);
        var provider = services.BuildServiceProvider();
        provider.GetService<IStaticMethodSource>().Should().BeOfType<SqliteEntityFrameworkStaticMethodProvider>();
    }
}