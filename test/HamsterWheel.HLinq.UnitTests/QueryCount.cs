using AutoFixture.Xunit2;
using FluentAssertions;
using HamsterWheel.HLinq.UnitTests.Dummies;

namespace HamsterWheel.HLinq.UnitTests;

partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [AutoData]
    public void Apply_WhenCount_ThenCanApply(DummyEntity[] entities)
    {
        //arrange
        var queryString = "count[]";
        var tokens = _tokenizer.Tokenize(queryString);
        var query = _parser.Parse<DummyEntity>(tokens, queryString);
        var queryable = entities.AsQueryable();

        //act
        var actual = _sut.ApplyGetType(queryable, query);

        //assert
        actual.Count.Should().Be(entities.Length);
    }
}