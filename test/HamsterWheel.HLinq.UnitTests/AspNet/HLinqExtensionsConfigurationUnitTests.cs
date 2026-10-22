using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.AspNet;

public class HLinqExtensionsConfigurationUnitTests
{
    [Fact]
    public void OverWriteWord_WhenCalled_ThenOldServiceImplementationsIsNotAvailable()
    {
        //arrange
        var expected = "";
        var config = new HLinqExtensionsConfiguration();
        var services = new ServiceCollection();
        var hLinqOptions = new HLinqOptions();
        HLinqCore.ConfigureServices(services, hLinqOptions);

        //act
        config.OverWriteWord<SelectWord, Select>();
        config.CustomServices[0](services);

        //assert
        var oldImplementations = services.Where(s => s.ServiceType == typeof(IHLinqWord) && s.ImplementationType == typeof(TokenWord<Select>));
        var implementations = services.Where(s => s.ServiceType == typeof(IHLinqWord) && s.ImplementationType == typeof(SelectWord));
        oldImplementations.Should().BeEmpty();
        implementations.Should().HaveCount(1)
            .And.Subject.First()
            .ImplementationType.Should().Be(typeof(SelectWord));
    }

    private sealed class SelectWord() : TokenWord<Select>("wybierz");
}