using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Demo.Extensions.Translations.pl;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.AspNet;

public class HLinqExtensionsConfigurationUnitTests
{
    [Fact]
    public void OverWriteTokenPossibility_WhenCalled_ThenOldServiceImplementationsIsNotAvailable()
    {
        //arrange
        var expected = "";
        var config = new HLinqExtensionsConfiguration();
        var services = new ServiceCollection();
        var hLinqOptions = new HLinqOptions();
        HLinqCore.ConfigureServices(services, hLinqOptions);

        //act
        config.OverWriteTokenPossibility<SelectPossibility, Select>();
        config.CustomServices[0](services);

        //assert
        var oldImplementations = services.Where(s => s.ServiceType == typeof(IHLinqTokenPossibility) && s.ImplementationType == typeof(TokenPossibility<Select>));
        var implementations = services.Where(s => s.ServiceType == typeof(IHLinqTokenPossibility) && s.ImplementationType == typeof(SelectPossibility));
        oldImplementations.Should().BeEmpty();
        implementations.Should().HaveCount(1)
            .And.Subject.First()
            .ImplementationType.Should().Be(typeof(SelectPossibility));
    }
}