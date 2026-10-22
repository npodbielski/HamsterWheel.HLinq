using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.MariaDb;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.MariaDb;

public class HLinqMariaDbExtensionsUnitTests
{
    [Fact]
    public void AddMariaDbToHLinq_WhenCalled_ThenRegistersCorrectServices()
    {
        //arrange
        var services = new ServiceCollection();
        var configuration = new HLinqConfiguration();

        //act
        var actual = configuration.AddMariaDbToHLinq();
        configuration.Extensions.CustomServices.ForEach(c => c(services));

        //assert
        actual.Should().Be(configuration);
        var provider = services.BuildServiceProvider();
        provider.GetService<IStaticMethodSource>().Should().BeOfType<MariaDbEntityFrameworkStaticMethodProvider>();
    }
}