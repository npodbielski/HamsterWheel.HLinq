using FluentAssertions;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.UnitTests.ValueConverters;

public class GuidValueConverterUnitTests
{
    [Fact]
    public void Convert_WhenDoubleQuoted_ThenCanConvert()
    {
        //arrange
        var guid = Guid.NewGuid();

        //act
        var actual = new GuidValueConverter().Convert(guid.ToString().Quote());

        //assert
        actual.Should().Be(guid);
    }

    [Fact]
    public void Convert_WhenUnquoted_ThenCanConvert()
    {
        //arrange
        var guid = Guid.NewGuid();

        //act
        var actual = new GuidValueConverter().Convert(guid.ToString());

        //assert
        actual.Should().Be(guid);
    }
}