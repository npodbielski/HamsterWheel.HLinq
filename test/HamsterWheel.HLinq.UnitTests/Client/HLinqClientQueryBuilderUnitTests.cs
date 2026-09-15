using FluentAssertions;
using HamsterWheel.HLinq.Client;

namespace HamsterWheel.HLinq.UnitTests.Client;

public class HLinqClientQueryBuilderUnitTests
{
    private readonly UnorderedHLinqClientQueryBuilder<Person> _sut = new HLinqClientQueryBuilderFactory().For<Person>();

    [Fact]
    public void Where_WhenCalledWithCondition_ThenCanBuildQuery()
    {
        //arrange
        var expected = "where[x.FirstName == Billy]";

        //act
        var actual = _sut.Where(x => x.FirstName == "Billy").Build();

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Where_WhenCalledWithConditionGroup_ThenCanBuildQuery()
    {
        //arrange
        var expected = "where[x.FirstName == Billy]";

        //act
        var actual = _sut.Where(x => x.FirstName == "Billy").Build();

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Where_WhenCalledWith2Groups_ThenCanBuildQuery()
    {
        //arrange
        var expected = "where[(x.FirstName == Billy && x.LastName == Montgomery) || (x.Id == 2 || x.Id == 3)]";

        //act
        var actual = _sut.Where(x => (x.FirstName == "Billy" && x.LastName == "Montgomery") || (x.Id == 2 || x.Id == 3))
            .Build();

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Where_WhenCalledWith1GroupWithAnd_ThenCanBuildQuery()
    {
        //arrange
        var expected = "where[x.FirstName == Billy && x.LastName == Montgomery]";

        //act
        var actual = _sut.Where(x => (x.FirstName == "Billy" && x.LastName == "Montgomery")).Build();

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Where_WhenCalledWith1GroupWithOr_ThenCanBuildQuery()
    {
        //arrange
        var expected = "where[x.FirstName == Billy || x.LastName == Montgomery]";

        //act
        var actual = _sut.Where(x => (x.FirstName == "Billy" || x.LastName == "Montgomery")).Build();

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Take_WhenCalledWithRange_ThenCanBuildQuery()
    {
        //act
        var actual = _sut.Take(10..20).Build();

        //assert
        actual.Should().Be("take[10..20]");
    }

    [Fact]
    public void Take_WhenCalledWithOpenEndedRange_ThenBuildsOpenEnd()
    {
        //act
        var actual = _sut.Take(10..).Build();

        //assert
        actual.Should().Be("take[10..]");
    }

    [Fact]
    public void Take_WhenCalledWithOpenStartedRange_ThenBuildsZeroStart()
    {
        //act
        var actual = _sut.Take(..20).Build();

        //assert - `0..20` means exactly the same as `..20` for the server
        actual.Should().Be("take[0..20]");
    }

    [Fact]
    public void Take_WhenCalledWithRangeAfterSkip_ThenSeparatesRootsWithDot()
    {
        //act
        var actual = _sut.Skip(5).Take(1..2).Build();

        //assert
        actual.Should().Be("skip[5].take[1..2]");
    }

    [Theory]
    [InlineData(1, 20, true, false)] // ^1..20 - start counted from the end
    [InlineData(1, 20, false, true)] // 1..^20 - end counted from the end
    public void Take_WhenCalledWithRangeFromEnd_ThenThrows(int start, int end, bool startFromEnd, bool endFromEnd)
    {
        //arrange
        var range = new Range(new Index(start, startFromEnd), new Index(end, endFromEnd));

        //act
        var action = () => _sut.Take(range).Build();

        //assert
        action.Should().Throw<ArgumentException>();
    }

    private class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? IpAddress { get; set; }
    }
}