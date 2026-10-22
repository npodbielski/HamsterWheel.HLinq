using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.MySql;

public class HLinqMySqlExtensionsUnitTests
{
    [Fact]
    public void AddMySqlToHLinq_WhenCalled_ThenRegistersCorrectServices()
    {
        //arrange
        var services = new ServiceCollection();
        var configuration = new HLinqConfiguration();

        //act
        var actual = configuration.AddMySqlToHLinq();
        configuration.Extensions.CustomServices.ForEach(c => c(services));

        //assert
        actual.Should().Be(configuration);
        var provider = services.BuildServiceProvider();
        provider.GetService<IStaticMethodSource>().Should().BeOfType<MySqlEntityFrameworkStaticMethodProvider>();
    }
}