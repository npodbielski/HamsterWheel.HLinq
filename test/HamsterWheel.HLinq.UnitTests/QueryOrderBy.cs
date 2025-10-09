using AutoFixture.Xunit2;
using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.Dummies;
using HamsterWheel.HLinq.UnitTests.Fixtures.AutoData;

namespace HamsterWheel.HLinq.UnitTests;

public partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [AutoData]
    public void Apply_WhenOrderBy_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        var queryString = "orderBy[x.DateTime]";
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
        actual.Should().BeEquivalentTo(entities.OrderBy(x => x.DateTime));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenOrderByThanBy_ThenCanApply([CollectionSize(20)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "orderBy[x.Enum].thenBy[x.Int]";
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
        actual.Should().BeEquivalentTo(entities.OrderBy(x => x.Enum).ThenBy(x => x.Int));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenOrderByThanByDescending_ThenCanApply([CollectionSize(20)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "orderBy[x.Enum].thenByDescending[x.Int]";
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
        actual.Should().BeEquivalentTo(entities.OrderBy(x => x.Enum).ThenByDescending(x => x.Int));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenOrderByDescending_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        var queryString = "orderByDescending[x.Int]";
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
        actual.Should().BeEquivalentTo(entities.OrderByDescending(x => x.Int));
    }
}