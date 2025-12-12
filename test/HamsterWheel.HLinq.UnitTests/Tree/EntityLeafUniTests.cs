using FluentAssertions;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree;

namespace HamsterWheel.HLinq.UnitTests.Tree;

public class EntityLeafUnitTests
{
    [Fact]
    public void GetValue_WhenCalledWithQueryString_ThenReturnsValue()
    {
        //arrange
        var expected = "x";
        var sut = new EntityLeaf(Entity.Build(new(0,1)));

        //act
        var actual = sut.GetValue("x");

        //assert
        actual.Should().Be(expected);
    }
}