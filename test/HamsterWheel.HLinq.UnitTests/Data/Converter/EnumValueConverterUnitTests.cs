using FluentAssertions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

public class EnumValueConverterUnitTests
{
    private readonly EnumValueConverter _sut = new();

    [Theory]
    [InlineData(FirstEnum.Value1, SecondEnum.Value1)]
    [InlineData(FirstEnum.Value2, SecondEnum.Value2)]
    [InlineData(FirstEnum.Value3, SecondEnum.Value3)]
    public void ConvertTo_WhenEnumStringRepresentationsMatch_ThenReturnsConvertedEnum(FirstEnum source, SecondEnum expected)
    {
        //act
        var actual = _sut.ConvertTo(source, typeof(SecondEnum));

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertTo_WhenEnumStringRepresentationsDoNotMatch_ThenThrowsException()
    {
        //arrange
        var action = () => _sut.ConvertTo(FirstEnum.OnlyInFirst, typeof(SecondEnum));

        //act && assert
        var actual = action.Should().Throw<InvalidConstantStringToTypeConversionException>();
        actual.WithMessage("* can not be converted to type *");
    }

    [Fact]
    public void CanConvert_WhenDestinationIsEnum_ThenReturnsTrue()
    {
        //act
        var actual = _sut.CanConvert(FirstEnum.Value1, typeof(SecondEnum));

        //assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void CanConvert_WhenDestinationIsNotEnum_ThenReturnsFalse()
    {
        //act
        var actual = _sut.CanConvert(FirstEnum.Value1, typeof(int));

        //assert
        actual.Should().BeFalse();
    }
}

public enum FirstEnum
{
    Value1 = 1,
    Value2 = 2,
    Value3 = 3,
    OnlyInFirst = 4
}

public enum SecondEnum
{
    Value1 = 10,
    Value2 = 20,
    Value3 = 30,
    OnlyInSecond = 40
}