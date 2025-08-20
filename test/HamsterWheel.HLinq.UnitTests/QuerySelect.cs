using AutoFixture.Xunit2;
using FluentAssertions;
using HamsterWheel.HLinq.UnitTests.Dummies;
using HamsterWheel.HLinq.UnitTests.Fixtures.AutoData;

namespace HamsterWheel.HLinq.UnitTests;

partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [AutoData]
    public void Apply_WhenSelect3Props_ThenCanApply([CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[x.Id,x.Name,x.Int]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Int
            })
        );
    }

    [Theory]
    [AutoData]
    public void Apply_WhenSelectOnePropFromConstWithDoubleQuotes_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[Directory=\"test\"]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities.Select(_ => new { Directory = "test" }));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenSelectOnePropFromTextConst_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[Directory=test]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities.Select(_ => new { Directory = "test" }));
    }

    [Theory]
    [AutoData]
    public void Apply_When2PropsOneFromTextConstAndSecondFromSource_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[Directory=test,x.id]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities.Select(x => new { Directory = "test", x.Id }));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenSelectOnePropFromConstWithSingleQuotes_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[Directory='t']";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities.Select(_ => new { Directory = 't' }));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenSelectOnePropFromIntegralConst_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[Number=1]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities.Select(_ => new { Number = 1 }));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenSelectOnePropFromFloatingPointConst_ThenCanApply(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var queryString = "select[Number=1.2323]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.Apply(queryable, query);

        //assert
        actual.Should().BeEquivalentTo(entities.Select(_ => new { Number = 1.2323 }));
    }
}