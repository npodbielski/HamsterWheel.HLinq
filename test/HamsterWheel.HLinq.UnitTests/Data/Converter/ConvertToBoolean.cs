using FluentAssertions;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

partial class DefaultConverterUnitTests
{
    [Theory]
    [InlineData("value", true)]
    [InlineData("1", true)]
    [InlineData("2", true)]
    [InlineData("t", true)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("yes", true)]
    [InlineData("a", true)]
    [InlineData("b", true)]
    [InlineData("ZZZZ", true)]
    [InlineData("0", false)]
    [InlineData("f", false)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("\0", false)]
    [InlineData("no", false)]
    [InlineData(" ", false)]
    [InlineData("    ", false)]
    [InlineData("\n", false)]
    [InlineData("\r", false)]
    [InlineData("\t", false)]
    [InlineData(null, false)]
    public void ConvertToBoolean_WhenCalledWithString_ThenReturnsCorrectValue(string? stringValue, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(stringValue, false);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("dsadsadaa", true)]
    [InlineData("test", true)]
    [InlineData("test tfdsdsa", true)]
    [InlineData("test 12312", true)]
    [InlineData("T", true)]
    [InlineData("F", false)]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    [InlineData("y", true)]
    [InlineData("n", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("Yes", true)]
    [InlineData("No", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("\t", false)]
    [InlineData("\n", false)]
    [InlineData("\r", false)]
    [InlineData("\r\n", false)]
    [InlineData("   \r\n \t", false)]
    public void ConvertToBool_WhenString_ReturnsFalseIfNullOrWhitespaceOrFalseEquivalent(object? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ConvertToBoolean_WhenCalledWithBool_ThenReturnsCorrectValue(bool value, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(null, false)]
    public void ConvertToBool_WhenNullableBool_ReturnsItsValue(bool? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [InlineData('1', true)]
    [InlineData('2', true)]
    [InlineData('a', true)]
    [InlineData('b', true)]
    [InlineData('t', true)]
    [InlineData('0', false)]
    [InlineData('f', false)]
    [InlineData('\0', false)]
    [InlineData(' ', false)]
    [InlineData('\r', false)]
    [InlineData('\n', false)]
    [InlineData('\t', false)]
    public void ConvertToBoolean_WhenCalledWithChar_ThenReturnsCorrectValue(char value, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData('a', true)]
    [InlineData('b', true)]
    [InlineData('T', true)]
    [InlineData('F', false)]
    [InlineData('t', true)]
    [InlineData('f', false)]
    [InlineData('y', true)]
    [InlineData('n', false)]
    [InlineData('Y', true)]
    [InlineData('N', false)]
    [InlineData('1', true)]
    [InlineData('0', false)]
    [InlineData(null, false)]
    [InlineData(' ', false)]
    [InlineData('\t', false)]
    [InlineData('\n', false)]
    [InlineData('\r', false)]
    [InlineData('\0', false)]
    public void ConvertToBool_WhenChar_ReturnsFalseIfNullOrWhitespaceOrFalseOrEquivalent(char? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(DateTimeData))]
    public void ConvertToBoolean_WhenCalledWithDateTime_ThenReturnsCorrectValue(DateTime value, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullableDateTimeData))]
    public void ConvertToBoolean_WhenCalledWithNullableDateTime_ThenReturnsCorrectValue(DateTime? value, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(DateTimeOffsetData))]
    public void ConvertToBoolean_WhenCalledWithDateTimeOffset_ThenReturnsCorrectValue(DateTimeOffset value,
        bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertToBoolean_WhenCalledWithCustomType_ThenReturnsCorrectValue()
    {
        //act
        var actual = _sut.ConvertToBoolean(new DummyEntity("test"), false);

        //assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void ConvertToBool_WhenObject_ReturnsTrueWhenNotNull()
    {
        _sut.ConvertToBoolean(new object(), false).Should().Be(true);
        _sut.ConvertToBoolean(new { test = "" }, false).Should().Be(true);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    public void ConvertToBool_WhenInt_ReturnsFalseWhenZero(int? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData((byte)0, false)]
    [InlineData((byte)1, true)]
    public void ConvertToBool_WhenByte_ReturnsFalseWhenZero(byte? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData((short)0, false)]
    [InlineData((short)1, true)]
    [InlineData((short)-1, true)]
    public void ConvertToBool_WhenShort_ReturnsFalseWhenZero(short? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0L, false)]
    [InlineData(1L, true)]
    [InlineData(-1L, true)]
    public void ConvertToBool_WhenLong_ReturnsFalseWhenZero(long? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0.0, false)]
    [InlineData(-0.5, true)]
    [InlineData(0.5, true)]
    [InlineData(1.0, true)]
    [InlineData(-1.0, true)]
    [InlineData(-1.5, true)]
    public void ConvertToBool_WhenDouble_ReturnsFalseWhenZero(double? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullableDecimalData))]
    public void ConvertToBool_WhenNullableDecimal_ReturnsFalseWhenZeroOrNull(decimal? value, bool expected)
    {
        _sut.ConvertToBoolean(value, false).Should().Be(expected);
    }

    public static TheoryData<decimal?, bool> NullableDecimalData
        => new()
        {
            { null, false },
            { 0m, false },
            { 0.5m, true },
            { -0.5m, true },
            { 1m, true },
            { -1m, true }
        };

    public static TheoryData<DateTimeOffset, bool> DateTimeOffsetData =>
        new()
        {
            { DateTimeOffset.MinValue, false },
            { DateTimeOffset.MaxValue, false },
            { DateTimeOffset.Now, true }
        };

    public static TheoryData<DateTime?, bool> NullableDateTimeData =>
        new()
        {
            { null, false },
            { DateTime.MinValue, false },
            { DateTime.MaxValue, false },
            { DateTime.Now, true }
        };
    
    public static TheoryData<DateTime, bool> DateTimeData =>
        new()
        {
            { DateTime.MinValue, false },
            { DateTime.MaxValue, false },
            { DateTime.Now, true }
        };
}