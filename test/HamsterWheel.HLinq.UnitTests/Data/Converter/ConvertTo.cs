using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

#pragma warning disable CA2263

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

partial class DefaultConverterUnitTests
{
    [Theory]
    [InlineData("value", "value")]
    [InlineData("1", "1")]
    public void ConvertTo_WhenCalledWithTheSameType_ThenReturnsCorrectValue(string stringValue, string expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(string), stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("null", null)]
    [InlineData("\"null\"", "null")]
    public void ConvertTo_WhenTargetIsString_ThenReturnsCorrectValue(string stringValue, string? expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(string), stringValue);

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
    [InlineData(null, 0)]
    public void ConvertTo_WhenCalledWithConvertableStringToInt_ThenReturnsCorrectValue(string? stringValue,
        int expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(int), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToDouble_ThenReturnsCorrectValue(string stringValue,
        double expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(double), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToDecimal_ThenReturnsCorrectValue(string stringValue,
        double expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(decimal), stringValue);

        //assert
        actual.Should().Be((decimal)expected);
    }

    [Fact]
    public void ConvertTo_WhenCalledWithMinValueDecimal_ThenReturnsCorrectValue()
    {
        //act
        var actual = _sut.ConvertTo(typeof(decimal), "-79228162514264337593543950335");

        //assert
        actual.Should().Be(decimal.MinValue);
    }

    [Fact]
    public void ConvertTo_WhenCalledWithMaxValueDecimal_ThenReturnsCorrectValue()
    {
        //act
        var actual = _sut.ConvertTo(typeof(decimal), "79228162514264337593543950335");

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
    public void ConvertTo_WhenCalledWithConvertableStringToByte_ThenReturnsCorrectValue(string stringValue,
        byte expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(byte), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToChar_ThenReturnsCorrectValue(string stringValue,
        char expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(char), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToShort_ThenReturnsCorrectValue(string stringValue,
        short expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(short), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToLong_ThenReturnsCorrectValue(string stringValue,
        long expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(long), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToDateTime_ThenReturnsCorrectValue(string stringValue,
        int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        //act
        var actual = _sut.ConvertTo(typeof(DateTime), stringValue);

        //assert
        actual.Should().Be(new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc));
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
    public void ConvertTo_WhenCalledWithConvertableStringToDateTimeOffset_ThenReturnsCorrectValue(string stringValue,
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
    public void ConvertTo_WhenCalledWithConvertableStringToSbyte_ThenReturnsCorrectValue(string stringValue,
        sbyte expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(sbyte), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToUshort_ThenReturnsCorrectValue(string stringValue,
        ushort expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(ushort), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToUint_ThenReturnsCorrectValue(string stringValue,
        uint expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(uint), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToBool_ThenReturnsCorrectValue(string? stringValue,
        bool expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(bool), stringValue);

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
    public void ConvertTo_WhenCalledWithConvertableStringToEnum_ThenReturnsCorrectValue(string stringValue,
        MethodSource expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(MethodSource), stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertTo_WhenCalledWithJsonStringToCustomType_ThenReturnsCorrectValue()
    {
        //arrange
        var entity = new NestedDummyEntity
        {
            Name = "test"
        };

        //act
        var actual = _sut.ConvertTo(typeof(NestedDummyEntity), JsonSerializer.Serialize(entity));

        //assert
        actual.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public void ConvertTo_WhenCalledWithJsonStringToCustomTypeAndCustomSerializer_ThenCallsCustomSerializer()
    {
        //arrange
        var fallbackConverter = new TestsFallbackConverter();
        var sut = GetSut(fallbackConverter);
        const string value = "{ \"name\": \"test\" }";

        //act
        var actual = sut.ConvertTo(typeof(DummyEntity), value);

        //assert
        fallbackConverter.Called.Should().BeTrue();
        actual.Should().BeEquivalentTo(JsonSerializer.Deserialize<DummyEntity>(value));
    }

    [Fact]
    public void ConvertTo_WhenCalledWithDifferentCustomType_ThenReturnsCorrectValue()
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

    /// <summary>
    /// The string form of a <see cref="DateTimeOffset" /> is the serializer's: the fraction of a
    /// second without its trailing zeros, whole fraction dropped when it is zero. Reading the
    /// string back is asserted along with the exact text, because the point of the shorter form is
    /// that it names the same instant.
    /// </summary>
    [Theory]
    [InlineData(1234567, "2026-01-01T12:00:00.1234567+02:00")]
    [InlineData(1234560, "2026-01-01T12:00:00.123456+02:00")]
    [InlineData(1234500, "2026-01-01T12:00:00.12345+02:00")]
    [InlineData(1200000, "2026-01-01T12:00:00.12+02:00")]
    [InlineData(0, "2026-01-01T12:00:00+02:00")]
    public void ConvertTo_WhenWithDateTimeOffsetToString_ThenReturnsCorrectValue(long ticks, string expected)
    {
        //arrange
        var dateTimeOffset = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromMinutes(120))
            .AddTicks(ticks);

        //act
        var actual = _sut.ConvertTo(typeof(string), dateTimeOffset);

        //assert
        actual.Should().Be(expected);
        DateTimeOffset.Parse((string)actual!, CultureInfo.InvariantCulture).Should().Be(dateTimeOffset);
    }

    /// <summary>
    /// The string form of a <see cref="DateTime" /> is the serializer's too: the fraction of a
    /// second without its trailing zeros, whole fraction dropped when it is zero - the same text
    /// a <see cref="DateTimeOffset" /> gets. Both forms name the instant, which is what a query
    /// compares on.
    /// </summary>
    [Theory]
    [InlineData(1234567, "2026-01-01T12:00:00.1234567Z")]
    [InlineData(1234560, "2026-01-01T12:00:00.123456Z")]
    [InlineData(0, "2026-01-01T12:00:00Z")]
    public void ConvertTo_WhenWithDateTimeToString_ThenReturnsCorrectValue(long ticks, string expected)
    {
        //arrange
        var dateTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(ticks);

        //act
        var actual = _sut.ConvertTo(typeof(string), dateTime);

        //assert
        actual.Should().Be(expected);
        DateTime.Parse((string)actual!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
            .Should().Be(dateTime);
    }

    [Fact]
    public void Convert_WhenDoubleQuoted_ThenCanConvert()
    {
        //arrange
        var guid = Guid.NewGuid();

        //act
        var actual = _sut.ConvertTo(typeof(Guid), guid.ToString().Quote());

        //assert
        actual.Should().Be(guid);
    }

    [Fact]
    public void Convert_WhenUnquoted_ThenCanConvert()
    {
        //arrange
        var guid = Guid.NewGuid();

        //act
        var actual = _sut.ConvertTo(typeof(Guid), guid.ToString());

        //assert
        actual.Should().Be(guid);
    }

    [Fact]
    public void ConvertTo_WhenNullAsGuid_ThenCanConvertToNull()
    {
        //arrange
        Guid? guid = null;

        //act
        var actual = _sut.ConvertTo(typeof(Guid?), new NullKeyword().Value);

        //assert
        actual.Should().Be(guid);
    }

    [Fact]
    public void ConvertTo_InfiniteRecursion_ThenCanConvert()
    {
        //arrange
        var source = new InfiniteRecursionClass
        {
            Name = "name"
        };
        source.Child = source;

        //act
        var actual = _sut.ConvertTo(typeof(Flat), source);

        //assert
        actual.Should().BeEquivalentTo(new Flat
        {
            Name = source.Name
        });
    }

    [Theory]
    //the quotes say the text is one value; which type reads it stays the property's, not the quotes'
    [InlineData("\"5\"", 5)]
    [InlineData("\"-2147483648\"", int.MinValue)]
    [InlineData("\" 1 \"", 1)]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsInt_ThenReturnsTheNumber(string stringValue, int expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(int), stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsNullableInt_ThenReturnsTheNumber()
    {
        //act
        var actual = _sut.ConvertTo(typeof(int?), "\"5\"");

        //assert
        actual.Should().Be(5);
    }

    [Theory]
    [InlineData("\"1.5\"", 1.5)]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsDouble_ThenReturnsTheNumber(string stringValue, double expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(double), stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsDecimal_ThenReturnsTheNumber()
    {
        //act
        var actual = _sut.ConvertTo(typeof(decimal), "\"1.5\"");

        //assert
        actual.Should().Be(1.5m);
    }

    [Fact]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsGuid_ThenReturnsTheGuid()
    {
        //act
        var actual = _sut.ConvertTo(typeof(Guid), "\"846e7ce6-ef91-43b9-877f-98ad793beca1\"");

        //assert
        actual.Should().Be(Guid.Parse("846e7ce6-ef91-43b9-877f-98ad793beca1"));
    }

    [Fact]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsDateTime_ThenReturnsTheDateTime()
    {
        //act
        var actual = _sut.ConvertTo(typeof(DateTime), "\"2020-01-01 00:00\"");

        //assert - the space is a character of the value and the date is read as the invariant one
        actual.Should().Be(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("\"1.2.3\"")]
    //the null keyword inside quotes is that word, and a word is not a number
    [InlineData("\"null\"")]
    public void ConvertTo_WhenQuotedValueIsNotTheType_ThenThrows(string stringValue)
    {
        //act
        var convert = () => _sut.ConvertTo(typeof(int), stringValue);

        //assert - the message names the text between the quotes, which is what was not a number
        convert.Should().Throw<InvalidConstantStringToTypeConversionException>()
            .And.Message.Should().Contain(stringValue.Trim('"')).And.Contain(nameof(Int32));
    }

    [Fact]
    public void ConvertTo_WhenValueIsQuotedAndTargetIsChar_ThenReturnsThatCharacter()
    {
        //act
        var actual = _sut.ConvertTo(typeof(char), "\"a\"");

        //assert - double quotes are taken off before the type reads the text, so a `char` gets one
        //character from a quoted value too
        actual.Should().Be('a');
    }

    [Theory]
    //what the culture calls a number: white space around it, a sign on either side, its own group
    //separator (the invariant one groups with a comma), and for the floating point types an exponent
    [InlineData(" 5 ", 5)]
    [InlineData("+5", 5)]
    [InlineData("5-", -5)]
    [InlineData("1,2", 12)]
    [InlineData("1,234", 1234)]
    public void ConvertTo_WhenTargetIsInt_ThenReadsWhatTheCultureCallsANumber(string stringValue, int expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(int), stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    //what the culture does not call a number: `Any` took a parenthesis for a minus and a currency
    //symbol for nothing at all
    [InlineData("(5)")]
    [InlineData("\u00A45")]
    [InlineData("5%")]
    [InlineData("1 000")]
    public void ConvertTo_WhenTargetIsIntAndTextIsNotItsNumber_ThenThrows(string stringValue)
    {
        //act
        var convert = () => _sut.ConvertTo(typeof(int), stringValue);

        //assert
        convert.Should().Throw<InvalidConstantStringToTypeConversionException>();
    }

    [Theory]
    [InlineData("1e3", 1000d)]
    [InlineData("1.5e2", 150d)]
    [InlineData("1.2", 1.2)]
    public void ConvertTo_WhenTargetIsDouble_ThenExponentAndDotStayFine(string stringValue, double expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(double), stringValue);

        //assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("(1.5)")]
    [InlineData("\u00A41.5")]
    public void ConvertTo_WhenTargetIsDecimalAndTextCarriesSignOrCurrency_ThenThrows(string stringValue)
    {
        //act
        var convert = () => _sut.ConvertTo(typeof(decimal), stringValue);

        //assert
        convert.Should().Throw<InvalidConstantStringToTypeConversionException>();
    }

    [Fact]
    public void ConvertTo_WhenTargetIsByte_ThenReadsTheInvariantNumber()
    {
        //act - `byte` used to parse with the culture of the machine, so which queries worked depended
        //on where the service ran
        var actual = _sut.ConvertTo(typeof(byte), "2,5");

        //assert
        actual.Should().Be((byte)25);
    }

    [Theory]
    //the culture the request named reads the text: the same characters are a different number where a
    //comma groups thousands instead of separating the fraction
    [InlineData("pl-PL", 1.2)]
    [InlineData("en-US", 12)]
    public void ConvertTo_WhenGivenACulture_ThenReadsTheValueInIt(string culture, decimal expected)
    {
        //act
        var actual = _sut.ConvertTo(typeof(decimal), "\"1,2\"", CultureInfo.GetCultureInfo(culture));

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ConvertTo_WhenGivenACulture_ThenReadsTheDateInIt()
    {
        //act - the month of the value is spelled in Polish
        var actual = _sut.ConvertTo(typeof(DateTime), "\"1 sty 2020\"", CultureInfo.GetCultureInfo("pl-PL"));

        //assert
        actual.Should().Be(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
    }

    [Fact]
    public void ConvertTo_WhenValueIsAnIntAndTargetIsEnum_ThenConvertsToTheMemberWithThatValue()
    {
        //act - the chain has to let the number reach the enum converter, and not read it as text first
        var actual = _sut.ConvertTo(typeof(SecondEnum), 10);

        //assert
        actual.Should().Be(SecondEnum.Value1);
    }

    [Fact]
    public void ConvertTo_WhenGivenNoCulture_ThenReadsTheValueInInvariantCulture()
    {
        //act - a request that named no culture reaches this converter as `null`, and `null` is the
        //invariant culture, not the one the service runs in
        var actual = _sut.ConvertTo(typeof(decimal), "\"1,2\"");

        //assert
        actual.Should().Be(12m);
    }

    [Theory]
    [InlineData("pl-PL", 1.2)]
    [InlineData("en-US", 12)]
    public void ConvertToT_WhenGivenACulture_ThenReadsTheValueInIt(string culture, decimal expected)
    {
        //act
        var actual = _sut.ConvertTo<decimal>("\"1,2\"", CultureInfo.GetCultureInfo(culture));

        //assert
        actual.Should().Be(expected);
    }

    public class InfiniteRecursionClass
    {
        public string Name { get; set; }
        public InfiniteRecursionClass Child { get; set; }
    }

    public class Flat
    {
        public string Name { get; set; }
    }
}