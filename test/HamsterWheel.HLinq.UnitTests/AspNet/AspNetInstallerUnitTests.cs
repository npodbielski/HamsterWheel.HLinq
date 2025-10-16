using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.AspNet;

public class AspNetInstallerUnitTests
{
    [Fact]
    public void ConfigureHLinq_WhenCalledWithConfigurationWithExtension_ThenRunsExtensionAction()
    {
        //arrange
        var services = new ServiceCollection();

        //act
        var actual = services.ConfigureHLinq(c => c.Extensions.CustomServices.Add(s => s.AddSingleton<Func<int>>(() => 1)))
            .BuildServiceProvider();

        //assert
        actual.GetService<Func<int>>().Should().NotBeNull();
        var service = actual.GetService<Func<int>>();
        service!().Should().Be(1);
    }
}