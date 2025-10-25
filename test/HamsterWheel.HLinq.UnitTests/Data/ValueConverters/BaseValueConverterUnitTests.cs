using FluentAssertions;
using HamsterWheel.HLinq.Data.ValueConverters;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests.Data.ValueConverters;

public class BaseValueConverterUnitTests
{
    [Fact]
    public void For_When_Then()
    {
        //arrange
        var sut = new TestBaseValueConverter();

        //act
        var actual = sut.For();

        //assert
        actual.Should().Be<int>();
    }

    [Fact]
    public void Convert_When_Then()
    {
        //arrange
        var sut = new TestBaseValueConverter();
        var action = () => sut.Convert("test");

        //act
        var actual = action.Should().Throw<InvalidConstantStringToTypeConversionException>();

        //assert
        actual.WithMessage("*can not be converted to type*");
    }
}

public class TestBaseValueConverter : BaseValueConverter<int>;