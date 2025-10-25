using FluentAssertions;
using HamsterWheel.HLinq.Data.ValueConverters;
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
    public override bool CanConvert(Type destination) => false;
}