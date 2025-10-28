using FluentAssertions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests.Data.ValueConverters;

public class BaseConfigurableValueConverterUnitTests
{
    [Fact]
    public void ConvertTo_When_Then()
    {
        //arrange
        var sut = new TestConfigurableValueConverter();
        var action = () => sut.ConvertTo("test", typeof(int));

        //act && assert
        var actual = action.Should().Throw<InvalidConstantStringToTypeConversionException>();
        actual.WithMessage("* can not be converted to type *");
    }
}

public class TestConfigurableValueConverter : BaseConfigurableValueConverter
{
    public override int Priority => 100000;
    public override bool CanConvert(object? value, Type destination) => false;
}