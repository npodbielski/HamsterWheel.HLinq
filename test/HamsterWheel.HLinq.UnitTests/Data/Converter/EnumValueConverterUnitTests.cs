using FluentAssertions;
using HamsterWheel.HLinq.Data;
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
    public void TryConvert_WhenEnumStringRepresentationsMatch_ThenReturnsConvertedEnum(FirstEnum source,
        SecondEnum expected)
    {
        //act
        var (converted, actual) = _sut.TryConvert(source, typeof(SecondEnum));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be(expected);
    }

    [Fact]
    public void TryConvert_WhenEnumStringRepresentationsDoNotMatch_ThenThrowsException()
    {
        //arrange
        var action = () => _sut.TryConvert(FirstEnum.OnlyInFirst, typeof(SecondEnum));

        //act && assert
        var actual = action.Should().Throw<InvalidConstantStringToTypeConversionException>();
        actual.WithMessage("* can not be converted to type *");
    }

    [Fact]
    public void TryConvert_WhenDestinationIsEnum_ThenConverts()
    {
        //act
        var (converted, actual) = _sut.TryConvert(FirstEnum.Value1, typeof(SecondEnum));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be(SecondEnum.Value1);
    }

    [Fact]
    public void TryConvert_WhenDestinationIsNotEnum_ThenHandsThePairToTheNextConverter()
    {
        //act
        var (converted, actual) = _sut.TryConvert(FirstEnum.Value1, typeof(int));

        //assert
        converted.Should().BeFalse();
        actual.Should().BeNull();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(10L)]
    [InlineData((short)10)]
    [InlineData((byte)10)]
    [InlineData(10d)]
    [InlineData(10f)]
    public void TryConvert_WhenValueIsANumber_ThenConvertsToTheMemberWithThatValue(object value)
    {
        //act
        var (converted, actual) = _sut.TryConvert(value, typeof(SecondEnum));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be(SecondEnum.Value1);
    }

    [Theory]
    [InlineData(10.5d)]
    [InlineData(10.5f)]
    public void TryConvert_WhenValueIsAFraction_ThenThrowsException(object value)
    {
        //arrange
        var action = () => _sut.TryConvert(value, typeof(SecondEnum));

        //act and assert
        action.Should().Throw<InvalidConstantStringToTypeConversionException>();
    }

    [Fact]
    public void TryConvert_WhenValueIsADecimalHoldingAWholeNumber_ThenConvertsToTheMemberWithThatValue()
    {
        //act - a `decimal` is the number it holds, not the text its culture would write
        var (converted, actual) = _sut.TryConvert(10m, typeof(SecondEnum));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be(SecondEnum.Value1);
    }

    [Fact]
    public void TryConvert_WhenValueIsADecimalFraction_ThenThrowsException()
    {
        //arrange
        var action = () => _sut.TryConvert(10.5m, typeof(SecondEnum));

        //act and assert
        action.Should().Throw<InvalidConstantStringToTypeConversionException>();
    }

    [Fact]
    public void TryConvert_WhenNumberDoesNotFitTheUnderlyingType_ThenThrowsException()
    {
        //arrange
        var action = () => _sut.TryConvert(300, typeof(ByteEnum));

        //act and assert - the number is not wrapped into the byte of the enum, it is refused
        action.Should().Throw<InvalidConstantStringToTypeConversionException>();
    }

    [Fact]
    public void TryConvert_WhenNumberHasNoMemberOfItsOwn_ThenTheNumberIsTheValue()
    {
        //act - as it is in the database, a value the enum does not name can still be asked for
        var (converted, actual) = _sut.TryConvert(99, typeof(SecondEnum));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be((SecondEnum)99);
    }

    [Fact]
    public void TryConvert_WhenValueIsANumberAndDestinationIsNullableEnum_ThenConverts()
    {
        //arrange
        var sut = new NullableEnumValueConverter(new NullKeyword(), new EnumValueConverter());

        //act
        var (converted, actual) = sut.TryConvert(10, typeof(SecondEnum?));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be(SecondEnum.Value1);
    }

    [Fact]
    public void TryConvert_WhenValueIsANumberOfAnotherEnumType_ThenReadsItsName()
    {
        //act - an enum value arrives as the name it was given, so the two enums meet by name
        var (converted, actual) = _sut.TryConvert(FirstEnum.Value2, typeof(SecondEnum));

        //assert
        converted.Should().BeTrue();
        actual.Should().Be(SecondEnum.Value2);
    }

}

public enum ByteEnum : byte;

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
}
