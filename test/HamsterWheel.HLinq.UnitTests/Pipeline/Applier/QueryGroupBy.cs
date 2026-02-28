using AutoFixture.Xunit2;
using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using HamsterWheel.HLinq.UnitTests.TestUtils.Fixtures.AutoData;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [AutoData]
    public void Apply_WhenGroupBy_ThenCanApply([CollectionSize(20)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "groupBy[x.Enum]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, hlinqQuery);

        //assert
        var actualGroups = ((object[])actual)
            .Cast<IGrouping<DummyEnum, DummyEntity>>()
            .OrderBy(g => g.Key)
            .ToList();
        var expectedGroups = entities.GroupBy(x => x.Enum).OrderBy(g => g.Key).ToList();
        actualGroups.Select(g => g.Key).Should().Equal(expectedGroups.Select(g => g.Key));
        for (var i = 0; i < expectedGroups.Count; i++)
        {
            actualGroups[i].Should().BeEquivalentTo(expectedGroups[i]);
        }
    }

    [Theory]
    [AutoData]
    public void Apply_WhenWhereAndGroupBy_ThenCanApply([CollectionSize(20)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "where[x.Flag==true].groupBy[x.Enum]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, hlinqQuery);

        //assert
        var actualGroups = ((object[])actual)
            .Cast<IGrouping<DummyEnum, DummyEntity>>()
            .OrderBy(g => g.Key)
            .ToList();
        var expectedGroups = entities
            .Where(x => x.Flag)
            .GroupBy(x => x.Enum)
            .OrderBy(g => g.Key)
            .ToList();
        actualGroups.Select(g => g.Key).Should().Equal(expectedGroups.Select(g => g.Key));
        for (var i = 0; i < expectedGroups.Count; i++)
        {
            actualGroups[i].Should().BeEquivalentTo(expectedGroups[i]);
        }
    }
}
