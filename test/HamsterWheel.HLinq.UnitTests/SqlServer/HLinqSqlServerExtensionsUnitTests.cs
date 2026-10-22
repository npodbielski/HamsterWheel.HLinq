using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.SqlServer;

public class HLinqSqlServerExtensionsUnitTests
{
    [Fact]
    public void AddSqlServerToHLinq_WhenCalled_ThenRegisterCorrectServices()
    {
        //arrange
        var services = new ServiceCollection();
        var config = new HLinqConfiguration();

        //act
        var actual = config.AddSqlServerToHLinq();
        config.Extensions.CustomServices.ForEach(c => c(services));

        //assert
        actual.Should().Be(config);
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IStaticMethodSource>();
        service.Should().NotBeNull();
        service.Should().BeOfType<SqlServerEntityFrameworkStaticMethodProvider>();
    }
}
