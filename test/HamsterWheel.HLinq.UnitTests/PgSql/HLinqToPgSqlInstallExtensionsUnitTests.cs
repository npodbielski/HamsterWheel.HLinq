using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.PgSql;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.PgSql;

public class HLinqToPgSqlInstallExtensionsUnitTests
{
    [Fact]
    public void AddHLingToPgSql_WhenCalled_ThenRegisterCorrectServices()
    {
        //arrange
        var services = new ServiceCollection();
        var config = new HLinqConfiguration();

        //act
        var actual = config.AddHLingToPgSql();
        config.Extensions.CustomServices.ForEach(c => c(services));

        //assert
        actual.Should().Be(config);
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IStaticMethodSource>();
        service.Should().NotBeNull();
        service.Should().BeOfType<EntityFrameworkStaticMethodProvider>();
    }
}