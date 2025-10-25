using FluentAssertions;
using HamsterWheel.HLinq.Data.ValueConverters;
using HamsterWheel.HLinq.UnitTests.Dummies;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.UnitTests.ValueConverters;

public class ConfigurableToPlainValueConverterUnitTests
{
    [Fact]
    public void For_WhenCalled_ThenReturnDestination()
    {
        //arrange
        var expected = typeof(DummyEnum);
        var sut = new ConfigurableToPlainValueConverter(expected, new EnumValueConverter());

        //act
        var actual = sut.For();

        //assert
        actual.Should().Be(expected);
    }
}