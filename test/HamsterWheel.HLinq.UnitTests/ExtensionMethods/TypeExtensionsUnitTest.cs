using FluentAssertions;

namespace HamsterWheel.HLinq.UnitTests;

public class TypeExtensionsUnitTests
{
    [Fact]
    public void IsGenericOf_WhenNotGeneric_ThenThrows()
    {
        //arrange
        var action = () => typeof(int).IsGenericOf(typeof(decimal));

        //act
        var exception = action.Should().Throw<ArgumentException>();

        //assert
        exception.WithMessage("*should be generic type*");
    }

    [Fact]
    public void IsGenericOf_WhenGenericOf_ThenTrue()
    {
        //arrange
        var expected = true;

        //act
        var actual = typeof(IList<int>).IsGenericOf(typeof(IList<>));

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void IsGenericOf_WhenNotGenericOf_ThenFalse()
    {
        //arrange
        var expected = false;

        //act
        var actual = typeof(Dictionary<int,int>).IsGenericOf(typeof(IList<>));

        //assert
        actual.Should().Be(expected);
    }
}