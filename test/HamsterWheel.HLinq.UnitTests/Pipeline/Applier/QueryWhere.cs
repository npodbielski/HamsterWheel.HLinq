using AutoFixture.Xunit3;
using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
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

    [Fact]
    public void Apply_WhenFilterComparesAMathRun_ThenTheRowComputesBeforeItIsCompared()
    {
        //arrange
        var entities = new[]
        {
            new DummyEntity("Ann") { Int = 10, Long = 3 },
            new DummyEntity("Bob") { Int = 7, Long = 1 }
        };
        const string queryString = "where[x.Int-x.Long>5]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(Json(entities.Where(e => e.Int - e.Long > 5)));
    }

    [Fact]
    public void Apply_WhenFilterComparesADecimalAndDoubleSum_ThenTheRowMeetsThemInADouble()
    {
        //arrange
        //so `x.Decimal+x.Double>=1e16` is the predicate the same Linq written by hand answers; the
        //decimal meets its double at 1e16, where a sum that kept the decimal's tail would answer
        //the row no
        var entities = new[]
        {
            new DummyEntity("Ann") { Decimal = 9999999999999999.99m, Double = 0.001 },
            new DummyEntity("Bob") { Decimal = 12.5m, Double = 3.5 }
        };
        const string queryString = "where[x.Decimal+x.Double>=10000000000000000]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(
            Json(entities.Where(e => (double)e.Decimal + e.Double >= 10000000000000000L)));
    }

    [Fact]
    public void Apply_WhenMathRunHangsOnACall_ThenTheCallJoinsTheRun()
    {
        //arrange
        var entities = new[]
        {
            new DummyEntity("Ann") { Text = "Lee" },
            new DummyEntity("Bob") { Text = "Day" }
        };
        const string queryString = "where[x.Name.ToUpper()+x.Text==ANNLEE]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(Json(entities.Where(e => e.Name.ToUpper() + e.Text == "ANNLEE")));
    }

    [Fact]
    public void Apply_WhenGroupOnAValueSideComputesTheComparedNumber_ThenTheRowIsComparedToIt()
    {
        //arrange
        //number is the moment - the very comparison `x.Int>6` writes directly
        var entities = new[]
        {
            new DummyEntity("Ann") { Int = 10 },
            new DummyEntity("Bob") { Int = 2 }
        };
        const string queryString = "where[x.Int>(2*3)]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(Json(entities.Where(e => e.Int > 2 * 3)));
    }

    [Fact]
    public void Apply_WhenFoldedConstantMeetsADate_ThenTheYearRuleReadsItLikeAWrittenNumber()
    {
        //arrange
        //reads the `2001` written directly: both start at the first moment of the year
        var entities = new[]
        {
            new DummyEntity("Ann") { DateTime = new DateTime(1998, 6, 1) },
            new DummyEntity("Bob") { DateTime = new DateTime(2005, 6, 1) }
        };
        const string queryString = "where[x.DateTime>(2*1000+1)]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(Json(entities.Where(e => e.DateTime > new DateTime(2001, 1, 1))));
    }

    [Fact]
    public void Apply_WhenDateSpelledAsSubtractionsStandsOnAValueSide_ThenItIsArithmeticNotADate()
    {
        //arrange
        //rule reads that number: the same moment `x.DateTime>1979` answers, and not the day
        var entities = new[]
        {
            new DummyEntity("Ann") { DateTime = new DateTime(1978, 6, 1) },
            new DummyEntity("Bob") { DateTime = new DateTime(1980, 6, 1) }
        };
        const string queryString = "where[x.DateTime>(2022-12-31)]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(Json(entities.Where(e => e.DateTime > new DateTime(1979, 1, 1))));
    }

    [Fact]
    public void Apply_WhenValueInitialComputationOpensTheCondition_ThenTheQueryIsRefused()
    {
        //arrange
        //dies in the window walk: the user moves the computation to the value side
        var entities = new[] { new DummyEntity("Ann") { Int = 10 } };
        const string queryString = "where[(2-1)>x.Int]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };

        //act
        var act = () =>
        {
            _parser.Parse(hlinqQuery, _tokenizer.Tokenize(queryString));
            return _sut.Apply(entities.AsQueryable(), hlinqQuery);
        };

        //assert
        act.Should().Throw<HLinqQueryException>();
    }

    [Fact]
    public void Apply_WhenDateFilterIsAYear_ThenTheYearStartsTheComparison()
    {
        //arrange
        //text written whole reads as a date, both spellings answer the same moment
        var entities = new[]
        {
            new DummyEntity("Ann") { DateTime = new DateTime(1978, 6, 1) },
            new DummyEntity("Bob") { DateTime = new DateTime(1980, 6, 1) }
        };
        const string queryString = "where[x.DateTime>1979]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        Json(actual).Should().Be(Json(entities.Where(e => e.DateTime > new DateTime(1979, 1, 1))));
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
    public void Apply_WhenQuotedValueIsComparedToInt_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Int>\"5\"]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => x.Int > 5));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenQuotedValueIsComparedToDateTime_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.DateTime>\"2025-12-01 00:00\"]";
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
            .Where(x => x.DateTime > new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc))
        );
    }

    [Theory]
    [InlineAutoData("Rock\"", "Rock\"", 1)]
    [InlineAutoData("\"", "\"", 1)]
    [InlineAutoData("\"\"", "\"\"", 1)]
    [InlineAutoData("John \"The Rock\" Stone", "John \"The Rock\" Stone", 1)]
    [InlineAutoData("a\"b\"c", "a\"b\"c", 1)]
    [InlineAutoData("Rock", "Rock\"", 0)]
    public void Apply_WhenQuotedValueHoldsQuotes_ThenCanApply(
        string actualName, string searchedName, int expectedCount, DummyEntity entity)
    {
        //arrange
        entity.Name = actualName;
        //a quote is part of the quoted value when no delimiter follows it, so the value is
        //wrapped in plain quotes: `"Rock""` searches for `Rock"`, because the value can only
        //end at the last of the two quotes, the one before `]`
        var queryString = $"where[x.Name==\"{searchedName}\"]";
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
    [InlineAutoData("Rock\"", 1)]
    [InlineAutoData("Rock", 0)]
    public void Apply_WhenValueEndingInQuoteContinuesConjunction_ThenCanApply(
        string actualName, int expectedCount, DummyEntity entity)
    {
        //arrange
        entity.Name = actualName;
        //the value commits at the second quote, the one the `&` of the conjunction follows
        const string queryString = "where[x.Name==\"Rock\"\"&&x.Int==1]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        entity.Int = 1;
        var queryable = new[] { entity }.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, hlinqQuery);

        //assert
        actual.Should().BeOfType<object[]>();
        ((object[])actual).Should().HaveCount(expectedCount);
    }

    [Theory]
    [AutoData]
    public void Apply_WhenQuotedValueIsTheNullKeyword_ThenItIsThatWord(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.NullableString==\"null\"]";
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
        actual.Should().BeEquivalentTo(entities.Where(x => x.NullableString == "null"));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenQuotedValueIsNotTheType_ThenThrows(DummyEntity[] entities)
    {
        //arrange
        const string queryString = "where[x.Int>\"abc\"]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        var queryable = entities.AsQueryable();

        //act
        var apply = () => _sut.Apply(queryable, hlinqQuery);

        //assert
        apply.Should().Throw<InvalidConstantStringToTypeConversionException>()
            .And.Message.Should().Contain("abc").And.Contain(nameof(Int32));
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
    [InlineAutoData("Kowalski, Jan", "Kowalski, Jan", 1)]
    [InlineAutoData("Kowalski, Jan", "Nowak, Adam", 0)]
    [InlineAutoData("a..b", "a..b", 1)]
    [InlineAutoData("a]b", "a]b", 1)]
    public void Apply_WhenQuotedValueHoldsDelimiters_ThenCanApply(
        string actualName, string searchedName, int expectedCount, DummyEntity entity)
    {
        //arrange
        entity.Name = actualName;
        var queryString = $"where[x.Name==\"{searchedName}\"]";
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
    /// `>=` and `&lt;=` followed through to the expression they build and the rows they return.
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

    [Fact]
    public void Apply_WhenIntComparedToItsNullable_ThenTheyMeetInTheNullableType()
    {
        //the same Linq written by hand compares through the lifted `==`: a row whose NullableInt
        //is null matches nothing, and that answer is C#'s own
        AssertFilterMatchesHandWrittenLinq("where[x.Int==x.NullableInt]", e => e.Int == e.NullableInt);
    }

    [Fact]
    public void Apply_WhenNullableIntComparedToInt_ThenTheyMeetInTheNullableType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.NullableInt==x.Int]", e => e.NullableInt == e.Int);
    }

    [Fact]
    public void Apply_WhenDecimalComparedToItsNullable_ThenTheyMeetInTheNullableType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Decimal==x.NullableDecimal]",
            e => e.Decimal == e.NullableDecimal);
    }

    [Fact]
    public void Apply_WhenDateTimeComparedToItsNullable_ThenTheyMeetInTheNullableType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.DateTime>x.NullableDateTime]",
            e => e.DateTime > e.NullableDateTime);
    }

    [Fact]
    public void Apply_WhenFlagComparedToItsNullable_ThenTheyMeetInTheNullableType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Flag==x.NullableFlag]", e => e.Flag == e.NullableFlag);
    }

    [Fact]
    public void Apply_WhenEnumComparedToItsNullable_ThenTheyMeetInTheNullableType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Enum==x.NullableEnum]", e => e.Enum == e.NullableEnum);
    }

    [Fact]
    public void Apply_WhenShortComparedToByte_ThenTheyMeetAfterTheNumericPromotion()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Short==x.Byte]", e => e.Short == e.Byte);
    }

    [Fact]
    public void Apply_WhenShortComparedGreaterThanByte_ThenTheyMeetAfterTheNumericPromotion()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Short>x.Byte]", e => e.Short > e.Byte);
    }

    [Fact]
    public void Apply_WhenIntComparedToLong_ThenTheyMeetInTheWiderType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Int==x.Long]", e => e.Int == e.Long);
    }

    [Fact]
    public void Apply_WhenFloatComparedToDouble_ThenTheyMeetInTheWiderType()
    {
        //the promotion lifts the float to a double; the rows hold halves a binary double and a
        //binary float both hold exactly, so the widened equality here compares without rounding
        AssertFilterMatchesHandWrittenLinq("where[x.Float==x.Double]",
            // ReSharper disable once CompareOfFloatsByEqualityOperator - the row values are exact
            e => e.Float == e.Double);
    }

    [Fact]
    public void Apply_WhenEnumComparedToInt_ThenTheyMeetInTheUnderlyingInteger()
    {
        //a query string has no cast, so the enum is converted to the number the store already
        //holds for it - the rows where the member's underlying value equals the int column
        AssertFilterMatchesHandWrittenLinq("where[x.Enum==x.Int]", e => (int)e.Enum == e.Int);
    }

    [Fact]
    public void Apply_WhenIntComparedToEnum_ThenTheyMeetInTheUnderlyingInteger()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Int==x.Enum]", e => e.Int == (int)e.Enum);
    }

    [Fact]
    public void Apply_WhenEnumComparedToNullableInt_ThenTheyMeetInTheLiftedUnderlying()
    {
        //the meeting and the nullable compose: an `Enum` and an `int?` compare as `int?`, so a
        //row whose NullableInt is null answers no
        AssertFilterMatchesHandWrittenLinq("where[x.Enum==x.NullableInt]", e => (int?)e.Enum == e.NullableInt);
    }

    [Fact]
    public void Apply_WhenEnumComparedToText_ThenTheyMeetInATextOfTheName()
    {
        //the name is what the engines speak and what a value standing against an enum member
        //is read as - a row whose Text holds the member's number answers no
        AssertFilterMatchesHandWrittenLinq("where[x.Enum==x.Text]",
            e => string.Equals(e.Enum.ToString(), e.Text, StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_WhenNullableEnumComparedToText_ThenANullMeetsNothing()
    {
        //a null enum spells the empty text - the same answer EF's own CASE gives a null on
        //every engine - so it meets no text, its own null included
        AssertFilterMatchesHandWrittenLinq("where[x.NullableEnum==x.Text]",
            e => e.NullableEnum.ToString() == e.Text);
    }

    [Fact]
    public void Apply_WhenDecimalComparedToDouble_ThenTheyMeetInADouble()
    {
        //C# refuses the pair because the cast could be written by hand; the query has no cast,
        //so the decimal meets the double as its nearest double
        AssertFilterMatchesHandWrittenLinq("where[x.Decimal==x.Double]",
            // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
            e => (double)e.Decimal == e.Double);
    }

    [Fact]
    public void Apply_WhenDecimalComparedToFloat_ThenTheyMeetInADouble()
    {
        //a `float` meets the `decimal` in the same double the `double` meets it in; the row
        //values are halves both binary types hold exactly
        AssertFilterMatchesHandWrittenLinq("where[x.Decimal==x.Float]",
            // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
            e => (double)e.Decimal == e.Float);
    }

    [Fact]
    public void Apply_WhenNullableDecimalComparedToDouble_ThenTheyMeetInANullableDouble()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.NullableDecimal==x.Double]",
            // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
            e => (double?)e.NullableDecimal == e.Double);
    }

    [Fact]
    public void Apply_WhenByteBackedEnumComparedToByte_ThenTheyMeetInTheUnderlyingByte()
    {
        //an enum meets the integer type it is declared with even when it is not `int`
        AssertFilterMatchesHandWrittenLinq("where[x.SmallEnum==x.Byte]", e => (byte)e.SmallEnum == e.Byte);
    }

    [Fact]
    public void Apply_WhenDecimalMeetsItsPastTheLoss_ThenTheTailTheDoubleCannotKeepStillMatches()
    {
        //the meeting's written loss, measured on the row: the decimal's last digits and the
        //double are not one value as decimals, yet met as doubles the decimal's tail is the
        //nearest double's and the row answers yes - the loss the meeting states, never a
        //rounding to a nearby integer the way C#'s own int-to-double widening never rounds
        var lossy = new[]
        {
            new DummyEntity("Loss")
            {
                Decimal = 9999999999999999.99m,
                Double = 1e+16
            }
        };

        //act
        var rows = Apply(lossy, "where[x.Decimal==x.Double]");

        //assert
        // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
        rows.Should().BeEquivalentTo(lossy.Where(e => (double)e.Decimal == e.Double));

        //and the two are not one value as decimals: only the meeting to doubles rounds away
        //the tail the double cannot keep
        lossy.Single().Decimal.Should().NotBe(10000000000000000m);
    }

    [Fact]
    public void Apply_WhenEnumComparedToAnotherEnum_ThenTheComparisonStaysRefused()
    {
        //two enums meet in nothing: neither is the underlying type of the other, and which
        //of their numbers the pair would compare is not stated anywhere
        var act = () => Apply(ComparisonRows(), "where[x.Enum==x.SmallEnum]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    [Fact]
    public void Apply_WhenEnumComparedToWiderIntegral_ThenTheyMeetInTheWidenedType()
    {
        //an enum meets any integral number, not only the width it is declared with: an
        //`enum:int` and a `long` meet where C# widens an `int` to meet a `long`
        AssertFilterMatchesHandWrittenLinq("where[x.Enum==x.Long]", e => (int)e.Enum == e.Long);
    }

    [Fact]
    public void Apply_WhenByteBackedEnumComparedToInt_ThenTheyMeetInTheWidenedType()
    {
        //a `byte`-backed enum and an `int` meet in `int`, the type the underlying `byte` widens
        //to and the type the number already is
        AssertFilterMatchesHandWrittenLinq("where[x.SmallEnum==x.Int]", e => (int)e.SmallEnum == e.Int);
    }

    [Fact]
    public void Apply_WhenByteBackedEnumComparedToLong_ThenTheyMeetInTheWidenedType()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.SmallEnum==x.Long]", e => (byte)e.SmallEnum == e.Long);
    }

    [Fact]
    public void Apply_WhenEnumComparedToUnsignedNumber_ThenTheComparisonStaysRefused()
    {
        //the underlying type and the number are C#'s own ambiguity once widened - an `enum:int`
        //and a `ulong` meet in no single type, so the pair keeps C#'s refusal
        var entities = new[] { new DummyEntity("Ann") { Enum = DummyEnum.One } };
        var act = () => Apply(entities, "where[x.Enum==x.Unsigned]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    [Fact]
    public void Apply_WhenByteComparedToUshort_ThenTheyMeetInTheIntCSharpComparesThemIn()
    {
        //neither width holds the other's values and `int` holds both: the covering number C#
        //itself binds the comparison of the two pair in
        AssertFilterMatchesHandWrittenLinq("where[x.Byte==x.Ushort]", e => e.Byte == e.Ushort);
    }

    [Fact]
    public void Apply_WhenUintComparedToByte_ThenTheyMeetInTheUintThatCoversBoth()
    {
        AssertFilterMatchesHandWrittenLinq("where[x.Uint==x.Byte]", e => e.Uint == e.Byte);
    }

    [Fact]
    public void Apply_WhenUintComparedToInt_ThenTheyMeetInTheLongThatCoversBoth()
    {
        //a uint holds values no int holds and the reverse, so the meeting widens past both
        AssertFilterMatchesHandWrittenLinq("where[x.Uint==x.Int]", e => e.Uint == e.Int);
    }

    [Fact]
    public void Apply_WhenSignedNumberComparedToUlong_ThenTheComparisonStaysRefused()
    {
        //no 64-bit number holds the values of a signed number and of a ulong, so the pair has
        //no meeting to state - the refusal C# itself gives the same pair written by hand
        var act = () => Apply(ComparisonRows(), "where[x.Int==x.Unsigned]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>()
            .WithMessage("*Int32*UInt64*");
    }

    [Fact]
    public void Apply_WhenUshortComparedToByte_ThenTheyMeetInAnInt()
    {
        //the meeting is the pair's, not the order's: the second width says nothing the first
        //already covered
        AssertFilterMatchesHandWrittenLinq("where[x.Ushort==x.Byte]", e => e.Ushort == e.Byte);
    }

    [Fact]
    public void Apply_WhenUlongComparedToLong_ThenTheComparisonStaysRefused()
    {
        //the refusal is the pair's, not the order's: no number covers both widths either way
        var act = () => Apply(ComparisonRows(), "where[x.Unsigned==x.Long]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>()
            .WithMessage("*UInt64*Int64*");
    }

    [Fact]
    public void Apply_WhenBoolComparedToText_ThenTheComparisonStaysRefused()
    {
        //the refusal a caller may expect to pass and that answers no: which text spells a
        //`bool` - "True", "true", "1" - is not one answer, so the pair stays refused
        var act = () => Apply(ComparisonRows(), "where[x.Flag==x.Text]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>()
            .WithMessage("*Boolean*String*");
    }

    [Fact]
    public void Apply_WhenGuidComparedToNumber_ThenTheComparisonStaysRefused()
    {
        var act = () => Apply(ComparisonRows(), "where[x.Id==x.Int]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    [Fact]
    public void Apply_WhenEnumerationComparedToText_ThenTheComparisonStaysRefused()
    {
        //an array and a text have no single meeting: the refusal stays
        var act = () => Apply(ComparisonRows(), "where[x.Children==x.Text]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>()
            .WithMessage("*List*String*");
    }

    [Fact]
    public void Apply_WhenEntityComparedToNumber_ThenTheComparisonStaysRefused()
    {
        var act = () => Apply(ComparisonRows(), "where[x.Nested==x.Int]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    [Fact]
    public void Apply_WhenEntityComparedToAnotherEntity_ThenTheComparisonStaysRefused()
    {
        var act = () => Apply(ComparisonRows(), "where[x.Nested==x.Children]");

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    /// <summary>
    /// A filter against the same predicate written by hand in LINQ over the rows.
    /// </summary>
    private void AssertFilterMatchesHandWrittenLinq(string queryString, Func<DummyEntity, bool> handWritten)
    {
        var entities = ComparisonRows();

        Apply(entities, queryString).Should().BeEquivalentTo(entities.Where(handWritten));
    }

    //the rows a query text builds and applies over the given ones
    private DummyEntity[] Apply(DummyEntity[] entities, string queryString)
    {
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        return [.. ((object[])_sut.Apply(entities.AsQueryable(), hlinqQuery)).Cast<DummyEntity>()];
    }

    //the rows of the compared pairs: one row whose every nullable holds the value its underlying
    //member holds, one whose every nullable is null - the lifted comparison must answer no there -
    //and one whose nullable and underlying members simply differ; the texts spell an enum member
    //by name on one row, by number on another (the answer of the name meeting is the name), and
    //one row keeps the text null
    private static DummyEntity[] ComparisonRows() =>
    [
        new("Ann")
        {
            Short = 300, Byte = 44, Ushort = 44, Int = 3, Uint = 44, Long = 3, Float = 3.5f, Double = 3.5, Decimal = 12.5m,
            Flag = true, Enum = DummyEnum.One, SmallEnum = DummyByteEnum.Little, Text = "One", DateTime = new(2020, 1, 1),
            NullableShort = 300, NullableByte = 44, NullableInt = 3, NullableLong = 3,
            NullableFloat = 3.5f, NullableDouble = 3.5, NullableDecimal = 12.5m,
            NullableFlag = true, NullableEnum = DummyEnum.One, NullableDateTime = new(2020, 1, 1)
        },
        new("Bob")
        {
            Short = 44, Byte = 44, Int = 3, Long = 3, Float = 3.5f, Double = 3.5, Decimal = 12.5m,
            Flag = true, Enum = DummyEnum.One, DateTime = new(2020, 1, 1),
            NullableShort = null, NullableByte = null, NullableInt = null, NullableLong = null,
            NullableFloat = null, NullableDouble = null, NullableDecimal = null,
            NullableFlag = null, NullableEnum = null, NullableDateTime = null
        },
        new("Cy")
        {
            Short = 3, Byte = 3, Ushort = 300, Int = 2, Uint = 5, Long = 7, Float = 7.25f, Double = 7.25, Decimal = 2.75m,
            Flag = false, Enum = DummyEnum.Two, SmallEnum = DummyByteEnum.Nothing, Text = "3", DateTime = new(2021, 6, 1),
            NullableShort = 4, NullableByte = 3, NullableInt = 7, NullableLong = 7,
            NullableFloat = 7.25f, NullableDouble = 7.25, NullableDecimal = 2.75m,
            NullableFlag = true, NullableEnum = DummyEnum.Zero, NullableDateTime = new(2021, 6, 1)
        }
    ];
}