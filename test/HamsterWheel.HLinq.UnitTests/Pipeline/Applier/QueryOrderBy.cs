using AutoFixture.Xunit3;
using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using HamsterWheel.HLinq.UnitTests.TestUtils.Fixtures.AutoData;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

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

    [Theory]
    [AutoData]
    public void Apply_WhenKeyIsAMathRun_ThenTheRowComputesTheKey(DummyEntity[] entities)
    {
        //arrange
        //computation hanging on a member: the same selector the same Linq written by hand builds
        var queryString = "orderBy[x.Int-x.Long]";
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
        actual.Should().BeEquivalentTo(entities.OrderBy(x => x.Int - x.Long));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenKeyOpensWithAGroup_ThenTheGroupIsComputedFirst([CollectionSize(20)] DummyEntity[] entities)
    {
        //arrange
        //the precedence the same expression written by hand keeps
        var queryString = "orderBy[(x.Int+x.Long)*2]";
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
        actual.Should().BeEquivalentTo(entities.OrderBy(x => (x.Int + x.Long) * 2));
    }

    [Fact]
    public void Apply_WhenKeyIsADecimalAndDoubleSum_ThenTheKeyIsTheDoubleTheMeetingAnswers()
    {
        //arrange
        //pair meets in the same double in all three; the first two rows meet in one double, so the
        //order of a key that kept the decimal's tail is the other order
        var entities = new[]
        {
            new DummyEntity("Ann") { Decimal = 10000000000000000.4m, Double = 0 },
            new DummyEntity("Bob") { Decimal = 10000000000000000m, Double = 0 },
            new DummyEntity("Cy") { Decimal = 2.5m, Double = 1.5 }
        };
        const string queryString = "orderBy[x.Decimal+x.Double]";
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);

        //act
        var actual = _sut.Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        //double apart and the order says which number the key was
        actual.Should().BeEquivalentTo(entities.OrderBy(e => (double)e.Decimal + e.Double),
            o => o.WithStrictOrdering());
    }

    [Theory]
    [AutoData]
    public void Apply_WhenDescendingKeyIsAMathRun_ThenTheComputedKeyOrdersDescending(DummyEntity[] entities)
    {
        //arrange
        var queryString = "orderByDescending[x.Int*2]";
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
        actual.Should().BeEquivalentTo(entities.OrderByDescending(x => x.Int * 2));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenMathKeyFollowsAPlainOne_ThenTheKeyStillOrdersTheRows([CollectionSize(20)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "orderBy[x.Enum].thenBy[x.Int-x.Long]";
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
        actual.Should().BeEquivalentTo(entities.OrderBy(x => x.Enum).ThenBy(x => x.Int - x.Long));
    }
}
