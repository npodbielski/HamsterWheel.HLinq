using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.UnitTests.ValueConverters;

public class BoolValueConverterUnitTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void Convert_WhenCalledWithConvertibleString_ThenReturnsBool(string str, bool expected)
    {
        //arrange
        //act
        var actual = new BoolValueConverter().Convert(str);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Convert_WhenCalledWithInvalidString_ThenThrows()
    {
        //arrange
        var stringValue = "dsadad";
        var action = () => new BoolValueConverter().Convert(stringValue);

        //act
        var actual = action.Should().Throw<InvalidConstantStringToTypeConversionException>();

        //assert
        actual.WithMessage($"Constant value of '{stringValue}' can not be converted to type*");
    }
}