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
    public void GroupBy_WhenCalledWithProperty_ThenCanBuildQuery()
    {
        //arrange
        var expected = "groupby[x.FirstName]";
        //act
        var actual = _sut.GroupBy(x => x.FirstName).Build();
        //assert
        actual.Should().Be(expected);
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