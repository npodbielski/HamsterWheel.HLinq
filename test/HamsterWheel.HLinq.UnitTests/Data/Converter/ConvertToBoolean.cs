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
    [MemberData(nameof(DateTimeData))]
    public void ConvertToBoolean_WhenCalledWithDateTime_ThenReturnsCorrectValue(DateTime value, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    public static TheoryData<DateTime, bool> DateTimeData =>
        new()
        {
            { DateTime.MinValue, false },
            { DateTime.MaxValue, false },
            { DateTime.Now, true }
        };

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

    public static TheoryData<DateTimeOffset, bool> DateTimeOffsetData =>
        new()
        {
            { DateTimeOffset.MinValue, false },
            { DateTimeOffset.MaxValue, false },
            { DateTimeOffset.Now, true }
        };

    [Theory]
    [MemberData(nameof(TimeOnlyData))]
    public void ConvertToBoolean_WhenCalledWithTimeOnly_ThenReturnsCorrectValue(TimeOnly value, bool expected)
    {
        //act
        var actual = _sut.ConvertToBoolean(value, false);

        //assert
        actual.Should().Be(expected);
    }

    public static TheoryData<TimeOnly, bool> TimeOnlyData =>
        new()
        {
            { TimeOnly.MinValue, false },
            { TimeOnly.MaxValue, false },
            { TimeOnly.FromDateTime(DateTime.Now), true }
        };

    [Fact]
    public void ConvertToBoolean_WhenCalledWithCustomTYpe_ThenReturnsCorrectValue()
    {
        //act
        var actual = _sut.ConvertToBoolean(new DummyEntity("test"), false);

        //assert
        actual.Should().BeTrue();
    }
}