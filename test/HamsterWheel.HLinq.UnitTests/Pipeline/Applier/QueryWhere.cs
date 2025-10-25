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
    public void Apply_WhenEmptyWhere_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        var queryString = "where[]";
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
        actual.Should().BeEquivalentTo(entities);
    }

    [Theory]
    [AutoData]
    public void Apply_WhenDateTimeFilter_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string dateTimeFilter = "2025-12-01T00:00:00.000Z";
        const string queryString = $"where[x.DateTime>{dateTimeFilter}]";
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
        actual.Should().BeEquivalentTo(entities
            .Where(x => x.DateTime > DateTime.Parse(dateTimeFilter))
        );
    }

    [Theory]
    [AutoData]
    public void Apply_WhenDateTimeOffsetFilter_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string dateTimeFilter = "2025-12-01T00:00:00.000Z";
        const string queryString = $"where[x.DateTimeOffset>{dateTimeFilter}]";
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
        actual.Should().BeEquivalentTo(entities
            .Where(x => x.DateTimeOffset > DateTime.Parse(dateTimeFilter))
        );
    }

    [Theory]
    [AutoData]
    public void Apply_WhenUnequal_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Int!=100]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => x.Int != 100));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenEnumOr_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Enum==Longer||x.Enum==Negative]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => x.Enum is DummyEnum.Longer or DummyEnum.Negative));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenEnumOrInGroup_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[(x.Enum==Longer||x.Enum==Negative)&&x.Int!=100]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => (x.Enum is DummyEnum.Longer or DummyEnum.Negative) && x.Int != 100));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenEqualsThenGreaterDate_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string dateTimeFilter = "2025-12-01T00:00:00.000Z";
        var queryString = $"where[x.Name=={entities[0].Name}&&x.DateTime>{dateTimeFilter}]";
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
        actual.Should().BeEquivalentTo(entities
            .Where(x =>
                x.Name == entities[0].Name &&
                x.DateTime > DateTime.Parse(dateTimeFilter))
        );
    }

    [Theory]
    [InlineAutoData("value", "test", 0)]
    [InlineAutoData("value", "value", 1)]
    [InlineAutoData("John Doe", "John Doe", 1)]
    public void Apply_WhenWhereOnly_ThenCanApply(string actualName, string searchedName, int expectedCount,
        DummyEntity entity)
    {
        //arrange
        entity.Name = actualName;
        var queryString = $"where[x.Name.Contains({searchedName})]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = new[] { entity }.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, hlinqQuery);

        //assert
        actual.Should().BeOfType<object[]>();
        ((object[])actual).Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineAutoData("RegenerateAllFilesFlow", 1)]
    [InlineAutoData("PostInstallFlow", 0)]
    public void Apply_WhenWhereWithTwoConditions_ThenCanApply(string actualName, int expectedCount, DummyEntity entity)
    {
        //arrange
        entity.Name = actualName;
        entity.Text = "core";
        const string queryString = "where[x.name==RegenerateAllFilesFlow && x.text==core]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = new[] { entity }.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, hlinqQuery);

        //assert
        actual.Should().BeOfType<object[]>();
        ((object[])actual).Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineAutoData("value", "test", 0)]
    [InlineAutoData("value", "value", 0)]
    public void Apply_WhenWhereAndSkip_ThenCanApply(string actualName, string searchedName, int expectedCount,
        DummyEntity[] entities)
    {
        //arrange
        var entity = entities[0];
        entity.Name = actualName;
        var queryString = $"where[x.Name.Contains({searchedName})].skip[1]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = new[] { entity }.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, hlinqQuery);

        //assert
        actual.Should().BeOfType<object[]>();
        ((object[])actual).Should().HaveCount(expectedCount);
    }

    [Theory]
    [AutoData]
    public void Apply_WhenWhereOrderByThanByDescendingSkipTake_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "where[x.Int>100].orderBy[x.Enum].thenByDescending[x.Int].skip[10].take[20]";
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
        actual.Should().BeEquivalentTo(entities
            .Where(x => x.Int > 100)
            .OrderBy(x => x.Enum).ThenByDescending(x => x.Int)
            .Skip(10)
            .Take(20)
        );
    }

    [Theory]
    [AutoData]
    public void Apply_WhenWhereOrderByThanByDescendingSkipTakeSelect_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString =
            "where[x.Int>100].orderBy[x.Enum].thenByDescending[x.Int].skip[10].take[20].select[x.Id,x.Name]";
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
        actual.Should().BeEquivalentTo(entities
            .Where(x => x.Int > 100)
            .OrderBy(x => x.Enum).ThenByDescending(x => x.Int)
            .Skip(10)
            .Take(20)
            .Select(x => new
            {
                x.Id,
                x.Name
            })
        );
    }
}