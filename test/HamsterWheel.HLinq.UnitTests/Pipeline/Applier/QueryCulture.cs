using AutoFixture.Xunit3;
using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using HamsterWheel.HLinq.UnitTests.TestUtils.Fixtures.AutoData;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A request names a culture, asks for none, or is refused.
/// </summary>
public partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [AutoData]
    public void Apply_WhenRequestNamesPolish_ThenReadsItsValuesInPolish(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var hLinqQuery = Parse("""culture=pl-PL&where[x.Decimal>"1,2"]""");

        //act
        var actual = NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //assert
        actual.Should().BeEquivalentTo(entities.Where(x => x.Decimal > 1.2m));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenRequestNamesAnEnglishCulture_ThenReadsItsValuesInIt(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var hLinqQuery = Parse("""culture=en-GB&where[x.Decimal>"1,2"]""");

        //act
        var actual = NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //assert
        actual.Should().BeEquivalentTo(entities.Where(x => x.Decimal > 12m));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenRequestNamesNoCulture_ThenReadsItsValuesInInvariantCulture(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var hLinqQuery = Parse("""where[x.Decimal>"1,2"]""");

        //act
        var actual = NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //assert
        actual.Should().BeEquivalentTo(entities.Where(x => x.Decimal > 12m));
    }

    [Theory]
    [AutoData]
    public void Apply_WhenRequestNamesTheInvariantCulture_ThenReadsItsValuesInIt(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var hLinqQuery = Parse("""culture=&where[x.Decimal>"1,2"]""");

        //act
        var actual = NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //assert
        actual.Should().BeEquivalentTo(entities.Where(x => x.Decimal > 12m));
    }

    [Theory]
    [InlineData("test")]
    [InlineData("ch-wakau")]
    [InlineData("not a culture")]
    public void Apply_WhenRequestNamesSomethingThatIsNotACulture_ThenThrows(string culture)
    {
        //arrange
        var hLinqQuery = Parse($"culture={culture}&where[x.Decimal>1]");
        var apply = () => NewApplier().Apply(Array.Empty<DummyEntity>().AsQueryable(), hLinqQuery);

        //act and assert
        apply.Should().Throw<UnknownCultureException>()
            .And.Message.Should().Contain(culture);
    }

    [Theory]
    [InlineData("")]
    [InlineData("culture=pl-PL&")]
    [InlineData("culture=en-GB&")]
    public void Apply_WhenAValueHoldsAnUnquotedComma_ThenTheQueryIsRefusedInEveryCulture(string culture)
    {
        //arrange
        //method, so here it separates two values where a filter expects one and the closing bracket
        var parse = () => Parse($"""{culture}where[x.Decimal>1,2]""");

        //act and assert - refused as a sequence of tokens, not converted into a value and then complained of
        parse.Should().Throw<InvalidTokenCollectionException>()
            .And.Message.Should().Contain("'1,2]'");
    }

    [Theory]
    [AutoData]
    public void Apply_WhenACommaCultureReadsADotValue_ThenItIsRefusedAndNotReadAsGrouped(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        //would answer a question nobody asked
        var hLinqQuery = Parse("""culture=pl-PL&where[x.Decimal>2.5]""");
        var apply = () => NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //act and assert
        apply.Should().Throw<InvalidConstantStringToTypeConversionException>()
            .And.Message.Should().Contain("2.5");
    }

    [Theory]
    [AutoData]
    public void Apply_WhenADotCultureReadsADotValue_ThenItIsReadAsAFraction(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange
        var hLinqQuery = Parse("""culture=en-GB&where[x.Decimal>2.5]""");

        //act
        var actual = NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //assert
        actual.Should().BeEquivalentTo(entities.Where(x => x.Decimal > 2.5m));
    }

    private static HLinqQueryApplier NewApplier() => ServicesCollection.NewApplier();

    private static HLinqQuery<DummyEntity> Parse(string request) =>
        HLinqQuery<DummyEntity>.Parse(ServicesCollection.BinderDependenciesBag, request);
}
