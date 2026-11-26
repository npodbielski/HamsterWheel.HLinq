using FluentAssertions;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

partial class DefaultConverterUnitTests
{
    [Fact]
    public void ConvertToInterface_WhenTypeImplementsTheInterface_ThenReturnsTheSameInstance()
    {
        //arrange
        var thing = new Thing();

        //act
        var actual = _sut.ConvertTo(typeof(IThing), thing);

        //assert
        actual.Should().BeSameAs(thing);
    }

    [Fact]
    public void ConvertToInterface_WhenListImplementsTheInterfaceOfSameArguments_ThenReturnsTheSameInstance()
    {
        //arrange
        var numbers = new List<int> { 1, 2, 3 };

        //act
        //the non-generic entry skips ConvertTo<T>'s own fast path, so the converter's own check matches
        var actual = _sut.ConvertTo(typeof(IReadOnlyList<int>), numbers);

        //assert
        actual.Should().BeSameAs(numbers);
    }

    [Fact]
    public void ConvertToInterface_WhenListOfObjectDoesNotMatchTheArguments_ThenConvertsViaSerialization()
    {
        //arrange
        var numbers = new List<object> { 1 };

        //act
        var actual = (object)_sut.ConvertTo<IReadOnlyList<decimal>>(numbers);

        //assert
        actual.Should().NotBeSameAs(numbers);
        ((IReadOnlyList<decimal>)actual).Should().Equal(1m);
    }

    [Fact]
    public void ConvertToInterface_WhenArrayOfObjectDoesNotMatchTheArguments_ThenConvertsViaSerialization()
    {
        //act
        var actual = _sut.ConvertTo<IReadOnlyList<decimal>>(new object[] { 1 });

        //assert
        actual.Should().Equal(1m);
    }

    public interface IThing;

    public sealed class Thing : IThing;
}
