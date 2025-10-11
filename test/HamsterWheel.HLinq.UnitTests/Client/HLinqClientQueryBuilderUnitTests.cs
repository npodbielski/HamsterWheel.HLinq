using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.UnitTests.Client;

public class HLinqClientQueryBuilderUnitTests
{
    private readonly UnorderedHLinqClientQueryBuilder<Person> _sut = new HLinqClientQueryBuilderFactory().For<Person>();

    [Fact]
    public void Where_WhenCalledWith2Groups_ThenCanBuildQuery()
    {
        //arrange
        var expected = "where[(x.FirstName == Billy && x.LastName == Montgomery) || (x.Id == 2 || x.Id == 3)]";

        //act
        var actual = _sut.Where(x => (x.FirstName == "Billy" && x.LastName == "Montgomery") || (x.Id == 2 || x.Id == 3)).Build();

        //assert
        actual.Should().Be(expected);
    }
}