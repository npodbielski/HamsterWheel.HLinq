using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Tree.Filter;
using HamsterWheel.HLinq.UnitTests.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

partial class DefaultConverterUnitTests
{
    [Theory]
    [InlineData("value", "value")]
    [InlineData("1", "1")]
    public void ConvertToT1_WhenCalledWithTheSameType_ThenReturnsCorrectValue(string stringValue, string expected)
    {
        //act
        var actual = _sut.ConvertTo<string>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("01", 1)]
    [InlineData("001", 1)]
    [InlineData(" 1", 1)]
    [InlineData("1 ", 1)]
    [InlineData(" 1 ", 1)]
    [InlineData("-2147483648", int.MinValue)]
    [InlineData("-147483648", -147483648)]
    [InlineData("-17483648", -17483648)]
    [InlineData("-1483648", -1483648)]
    [InlineData("-183648", -183648)]
    [InlineData("-13648", -13648)]
    [InlineData("-1648", -1648)]
    [InlineData("-148", -148)]
    [InlineData("-18", -18)]
    [InlineData("-1", -1)]
    [InlineData("2", 2)]
    [InlineData("30", 30)]
    [InlineData("410", 410)]
    [InlineData("5323", 5323)]
    [InlineData("67231", 67231)]
    [InlineData("732021", 732021)]
    [InlineData("8232098", 8232098)]
    [InlineData("90092311", 90092311)]
    [InlineData("102323281", 102323281)]
    [InlineData("2147483647", int.MaxValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToInt_ThenReturnsCorrectValue(string stringValue,
        int expected)
    {
        //act
        var actual = _sut.ConvertTo<int>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("01", 1)]
    [InlineData("001", 1)]
    [InlineData("1.0", 1.0)]
    [InlineData("-1.0", -1.0)]
    [InlineData(".00000000000001", 0.00000000000001)]
    [InlineData("-179769313486231570000000000000000000000000000000000000",
        -179769313486231570000000000000000000000000000000000000.0)]
    [InlineData("179769313486231570000000000000000000000000000000000000",
        179769313486231570000000000000000000000000000000000000.0)]
    public void ConvertToT1_WhenCalledWithConvertableStringToDouble_ThenReturnsCorrectValue(string stringValue,
        double expected)
    {
        //act
        var actual = _sut.ConvertTo<double>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("01", 1)]
    [InlineData("001", 1)]
    [InlineData("1.0", 1.0)]
    [InlineData("-1.0", -1.0)]
    [InlineData(".00000000000001", 0.00000000000001)]
    public void ConvertToT1_WhenCalledWithConvertableStringToDecimal_ThenReturnsCorrectValue(string stringValue,
        double expected)
    {
        //act
        var actual = _sut.ConvertTo<decimal>(stringValue);

        //assert
        actual.Should().Be((decimal)expected);
    }

    [Fact]
    public void ConvertToT1_WhenCalledWithMinValueDecimal_ThenReturnsCorrectValue()
    {
        //act
        var actual = _sut.ConvertTo<decimal>("-79228162514264337593543950335");

        //assert
        actual.Should().Be(decimal.MinValue);
    }

    [Fact]
    public void ConvertToT1_WhenCalledWithMaxValueDecimal_ThenReturnsCorrectValue()
    {
        //act
        var actual = _sut.ConvertTo<decimal>("79228162514264337593543950335");

        //assert
        actual.Should().Be(decimal.MaxValue);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("003", 3)]
    [InlineData("04", 4)]
    [InlineData("255", byte.MaxValue)]
    [InlineData("0", byte.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToByte_ThenReturnsCorrectValue(string stringValue,
        byte expected)
    {
        //act
        var actual = _sut.ConvertTo<byte>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("a", 'a')]
    [InlineData("b", 'b')]
    [InlineData("X", 'X')]
    [InlineData("-", '-')]
    [InlineData("\uFFFF", char.MaxValue)]
    [InlineData("\0", char.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToChar_ThenReturnsCorrectValue(string stringValue,
        char expected)
    {
        //act
        var actual = _sut.ConvertTo<char>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("003", 3)]
    [InlineData("04", 4)]
    [InlineData("32767", short.MaxValue)]
    [InlineData("-32768", short.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToShort_ThenReturnsCorrectValue(string stringValue,
        short expected)
    {
        //act
        var actual = _sut.ConvertTo<short>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("003", 3)]
    [InlineData("04", 4)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("-9223372036854775808", long.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToLong_ThenReturnsCorrectValue(string stringValue,
        long expected)
    {
        //act
        var actual = _sut.ConvertTo<long>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("2021-10-10", 2021, 10, 10, 0, 0, 0, 0)]
    [InlineData("2025-01-01", 2025, 1, 1, 0, 0, 0, 0)]
    [InlineData("2003-12-31 10:10", 2003, 12, 31, 10, 10, 0, 0)]
    [InlineData("1904-03-21 23:59:01.001", 1904, 3, 21, 23, 59, 1, 1)]
    [InlineData("2999-12-31", 2999, 12, 31, 0, 0, 0, 0)]
    [InlineData("0001-01-01", 0001, 01, 01, 0, 0, 0, 0)]
    [InlineData("2025/01/01", 2025, 1, 1, 0, 0, 0, 0)]
    [InlineData("2025.11.22", 2025, 11, 22, 0, 0, 0, 0)]
    public void ConvertToT1_WhenCalledWithConvertableStringToDateTime_ThenReturnsCorrectValue(string stringValue,
        int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        //act
        var actual = _sut.ConvertTo<DateTime>(stringValue);

        //assert
        actual.Should().Be(new DateTime(year, month, day, hour, minute, second, millisecond));
    }

    [Theory]
    [InlineData("2021-10-10", 2021, 10, 10, 0, 0, 0, 0)]
    [InlineData("2025-01-01", 2025, 1, 1, 0, 0, 0, 0)]
    [InlineData("2003-12-31 10:10", 2003, 12, 31, 10, 10, 0, 0)]
    [InlineData("1904-03-21 23:59:01+01:00", 1904, 3, 21, 23, 59, 1, 60)]
    [InlineData("2999-12-31", 2999, 12, 31, 0, 0, 0, 0)]
    [InlineData("0002-01-01", 0002, 01, 01, 0, 0, 0, 0)]
    [InlineData("2025/01/01", 2025, 1, 1, 0, 0, 0, 0)]
    [InlineData("2025.11.22", 2025, 11, 22, 0, 0, 0, 0)]
    [InlineData("2025-01-01 00:00:00 +01:00", 2025, 1, 1, 0, 0, 0, 60)]
    public void ConvertToT1_WhenCalledWithConvertableStringToDateTimeOffset_ThenReturnsCorrectValue(string stringValue,
        int year, int month, int day, int hour, int minute, int second, int zone)
    {
        //act
        var actual = _sut.ConvertTo<DateTimeOffset>(stringValue);

        //assert
        actual.Should().Be(new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.FromMinutes(zone)));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("003", 3)]
    [InlineData("04", 4)]
    [InlineData("127", sbyte.MaxValue)]
    [InlineData("-128", sbyte.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToSbyte_ThenReturnsCorrectValue(string stringValue,
        sbyte expected)
    {
        //act
        var actual = _sut.ConvertTo<sbyte>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("003", 3)]
    [InlineData("04", 4)]
    [InlineData("65535", ushort.MaxValue)]
    [InlineData("0", ushort.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToUshort_ThenReturnsCorrectValue(string stringValue,
        ushort expected)
    {
        //act
        var actual = _sut.ConvertTo<ushort>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("003", 3)]
    [InlineData("04", 4)]
    [InlineData("4294967295", uint.MaxValue)]
    [InlineData("0", uint.MinValue)]
    public void ConvertToT1_WhenCalledWithConvertableStringToUint_ThenReturnsCorrectValue(string stringValue,
        uint expected)
    {
        //act
        var actual = _sut.ConvertTo<uint>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\0", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("               ", false)]
    [InlineData("\r", false)]
    [InlineData("\n", false)]
    [InlineData("\t", false)]
    [InlineData("a", true)]
    [InlineData("1", true)]
    [InlineData("2", true)]
    [InlineData("0", false)]
    [InlineData("10", true)]
    public void ConvertToT1_WhenCalledWithConvertableStringToBool_ThenReturnsCorrectValue(string? stringValue,
        bool expected)
    {
        //act
        var actual = _sut.ConvertTo<bool>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(MethodSource.Constant), MethodSource.Constant)]
    [InlineData(nameof(MethodSource.Property), MethodSource.Property)]
    [InlineData(nameof(MethodSource.Static), MethodSource.Static)]
    [InlineData("0", MethodSource.Property)]
    [InlineData("1", MethodSource.Constant)]
    [InlineData("2", MethodSource.Static)]
    public void ConvertToT1_WhenCalledWithConvertableStringToEnum_ThenReturnsCorrectValue(string stringValue,
        MethodSource expected)
    {
        //act
        var actual = _sut.ConvertTo<MethodSource>(stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertToT1_WhenCalledWithJsonStringToCustomType_ThenReturnsCorrectValue()
    {
        //arrange
        var entity = new NestedDummyEntity
        {
            Name = "test"
        };

        //act
        var actual = _sut.ConvertTo<NestedDummyEntity>(JsonSerializer.Serialize(entity));

        //assert
        actual.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void ConvertToT1_WhenCalledWithJsonStringToCustomTypeAndCustomSerializer_ThenCallsCustomSerializer()
    {
        //arrange
        var fallbackConverter = new FallbackConverter();
        var sut = GetSut(fallbackConverter);
        var value = "{ \"name\": \"test\" }";

        //act
        var actual = sut.ConvertTo<DummyEntity>(value);

        //assert
        fallbackConverter.Called.Should().BeTrue();
        actual.Should().BeEquivalentTo(JsonSerializer.Deserialize<DummyEntity>(value));
    }

    [Fact]
    public void ConvertToT1_WhenCalledWithDifferentCustomType_ThenReturnsCorrectValue()
    {
        //arrange
        var entity = new NameOnlyEntity
        {
            Name = "test"
        };

        //act
        var actual = _sut.ConvertTo<NestedDummyEntity>(entity);

        //assert
        actual.Should().BeEquivalentTo(new NestedDummyEntity
        {
            Name = entity.Name
        });
    }
}