using FluentAssertions;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

partial class DefaultConverterUnitTests
{
    [Fact]
    public void NeedConversion_WhenStringAndNullReturnsFalls()
    {
        //act
        var actual = _sut.NeedConversion(typeof(string), null);

        //assert
        actual.Should().Be(false);
    }

    [Fact]
    public void NeedConversion_WhenClassAndNullReturnsFalls()
    {
        //act
        var actual = _sut.NeedConversion(typeof(DefaultConverterUnitTests), null);

        //assert
        actual.Should().Be(false);
    }
}