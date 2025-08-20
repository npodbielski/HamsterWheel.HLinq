using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.AspNet.Binder;
using HamsterWheel.HLinq.Builders;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.AspNet;

public class AspNetInstallerUnitTests
{
    [Theory]
    [InlineData(typeof(IHLinqCore))]
    [InlineData(typeof(WebHLinqQueryBinder))]
    [InlineData(typeof(IHLinqQueryBinder))]
    public void ConfigureHLinq_WhenCalledWithoutConfiguration_ThenRequiredServiceIsRegistered(Type type)
    {
        //arrange
        var expected = "";
        var services = new ServiceCollection();

        //act
        var actual = services.ConfigureHLinq().BuildServiceProvider();

        //assert
        actual.GetService(type).Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHLinq_WhenCalledWithConfigurationWithApiServices_ThenUsesStaticMethodSourceFromApiProvider()
    {
        //arrange
        var services = new ServiceCollection();
        var methodSource = Substitute.For<IStaticMethodSource>();
        methodSource.Types.Returns([typeof(string)]);
        var apiServices = new ServiceCollection().AddSingleton(methodSource).BuildServiceProvider();

        //act
        var actual = services.ConfigureHLinq(c => c.ApiServices = apiServices).BuildServiceProvider();

        //assert
        actual.GetService<IStaticMethodSource>().Should().NotBeNull();
        actual.GetService<IStaticMethodSource>()!.Types.Should().BeEquivalentTo([typeof(string)]);
    }

    [Fact]
    public void ConfigureHLinq_WhenCalledWithConfigurationWithExtension_ThenRunsExtensionAction()
    {
        //arrange
        var services = new ServiceCollection();

        //act
        var actual = services.ConfigureHLinq(c => c.Extensions.Add(s => s.AddSingleton<Func<int>>(() => 1)))
            .BuildServiceProvider();

        //assert
        actual.GetService<Func<int>>().Should().NotBeNull();
        var service = actual.GetService<Func<int>>();
        service!().Should().Be(1);
    }
}