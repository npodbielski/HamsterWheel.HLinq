using FluentAssertions;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Tokens;

public class CommaUnitTests
{
    [Fact]
    public void Ctor_WhenCalled_ThenCreatesInstance()
    {
        //arrange
        //act
        var actual = new Comma();

        //assert
        actual.Should().BeAssignableTo<TokenBase>();
    }

    [Fact]
    public void Empty_WhenCalled_ReturnsTheSameInstance()
    {
        //arrange
        //act
        var actual = Comma.Empty;

        //assert
        actual.Should().BeSameAs(Comma.Empty);
    }

    [Fact]
    public void Build_WhenCalled_ReturnsInstanceWithRange()
    {
        //arrange
        var expected = 0..1;

        //act
        var actual = Comma.Build(new(new(expected.Start.Value), new(expected.End.Value)));

        //assert
        var range = actual.Should().BeOfType<Comma>().Which.Range;
        range.Start.Value.Should().Be(expected.Start.Value);
        range.End.Value.Should().Be(expected.End.Value);
    }
}