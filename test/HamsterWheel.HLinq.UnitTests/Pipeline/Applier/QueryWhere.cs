using AutoFixture.Xunit3;
using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Filtering;
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
    public void Apply_WhenValueComparedWithIntReversed_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[100<x.Int]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => 100 < x.Int));
    }

    [Theory]
    [InlineAutoData("John Doe")]
    public void Apply_WhenValueComparedWithStringReversed_ThenCanApply(string name, DummyEntity entity)
    {
        //arrange
        entity.Name = name;
        var queryString = $"where[{name}==x.Name]";
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
        actual.Should().BeEquivalentTo(new[] { entity });
    }

    [Theory]
    [AutoData]
    public void Apply_WhenValueComparedWithDateReversed_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string dateTimeFilter = "2025-12-01T00:00:00.000Z";
        var queryString = $"where[{dateTimeFilter}<x.DateTime]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => DateTime.Parse(dateTimeFilter) < x.DateTime));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenReversedComparisonAndAnotherCondition_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[100<x.Int&&x.Name==Jan]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => 100 < x.Int && x.Name == "Jan"));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenReversedComparisonWithSelect_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[100<x.Int].select[x.Name]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => 100 < x.Int).Select(x => x.Name));
    }

    /// <summary>
    ///     The comparisons that end in `=`: the tokenizer accepted them all along, but `>=` and `<=`
    ///     had never been followed through to the expression they build and the rows they return.
    /// </summary>
    [Theory]
    [AutoData]
    public void Apply_WhenIntGreaterOrEqualThanValue_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Int>=100]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => x.Int >= 100));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenIntLessOrEqualThanValue_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Int<=100]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => x.Int <= 100));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenOnlyValuesAreCompared_ThenThrows(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[1==1]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = entities.AsQueryable();

        //act
        var act = () => _sut.Apply(queryable, hlinqQuery);

        //assert
        act.Should().Throw<Condition.ConditionElementMustHaveLeftAndRightException>();
    }

    [Theory]
    [AutoData]
    public void Apply_WhenPropertiesOfDifferentTypesAreCompared_ThenThrows(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Name==x.DateTime]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = entities.AsQueryable();

        //act
        var act = () => _sut.Apply(queryable, hlinqQuery);

        //assert
        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
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