using FluentAssertions;

namespace HamsterWheel.HLinq.UnitTests;

public class StringExtensionsUnitTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("b", "b")]
    [InlineData("c", "c")]
    [InlineData("test", "test")]
    [InlineData("Test", "test")]
    [InlineData("A", "a")]
    [InlineData("B", "b")]
    [InlineData("C", "c")]
    [InlineData(nameof(StringExtensionsUnitTests), "stringExtensionsUnitTests")]
    public void ToCamelCase_WhenCalledWithString_ThenReturnsExpected(string original, string expected)
    {
        //arrange && act && assert
        original.ToCamelCase().Should().Be(expected);
    }
}