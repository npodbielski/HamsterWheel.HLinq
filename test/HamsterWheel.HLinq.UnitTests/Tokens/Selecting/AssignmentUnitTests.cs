using FluentAssertions;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Tokens.Selecting;

public class AssignmentUnitTests
{
    [Fact]
    public void Ctor_WhenCalled_ThenCreatesInstance()
    {
        //arrange
        //act
        var actual = new Assignment();

        //assert
        actual.Should().BeAssignableTo<TokenBase>();
    }

    [Fact]
    public void Empty_WhenCalled_ReturnsTheSameInstance()
    {
        //arrange
        //act
        var actual = Assignment.Empty;

        //assert
        actual.Should().BeSameAs(Assignment.Empty);
    }

    [Fact]
    public void Build_WhenCalled_ReturnsInstanceWithRange()
    {
        //arrange
        var expected = 0..1;

        //act
        var actual = Assignment.Build(new(new(expected.Start.Value), new(expected.End.Value)));

        //assert
        var range = actual.Should().BeOfType<Assignment>().Which.Range;
        range.Start.Value.Should().Be(expected.Start.Value);
        range.End.Value.Should().Be(expected.End.Value);
    }
}