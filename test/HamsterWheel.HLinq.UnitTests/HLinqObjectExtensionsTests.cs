using FluentAssertions;

namespace HamsterWheel.HLinq.UnitTests;

public class HLinqObjectExtensionsTests
{
    [Fact]
    public void ExecuteHLinq_WhenSelectOnSingleObject_ThenReturnsCorrectData()
    {
        //arrange
        const string expected = "aa";
        var obj = new
        {
            Name = expected
        };

        //act
        var actual = obj.ExecuteHLinq("select[x.Name]");

        //assert
        actual.Should().NotBeNull();
        actual!.Should().Be(expected);
    }

    [Fact]
    public void ExecuteHLinq_WhenSelectOnSingleObjectWithoutSelect_ThenReturnsCorrectData()
    {
        //arrange
        const string expected = "aa";
        var obj = new
        {
            Name = expected
        };

        //act
        var actual = obj.ExecuteHLinq("x.Name");

        //assert
        actual.Should().NotBeNull();
        actual.Should().Be(expected);
    }
}