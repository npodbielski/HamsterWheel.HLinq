using AutoFixture.Xunit3;
using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using HamsterWheel.HLinq.UnitTests.TestUtils.Fixtures.AutoData;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// Any culture the runtime knows is a culture a request may ask for. A request that names nothing is
/// read in the invariant culture and a request that names a name is refused, so no query is ever read
/// in a culture nobody asked for.
/// </summary>
public partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [AutoData]
    public void Apply_WhenRequestNamesPolish_ThenReadsItsValuesInPolish(
        [CollectionSize(100)] DummyEntity[] entities)
    {
        //arrange - there a comma is where the fraction starts
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
        //arrange - and there the same characters group thousands
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

        //assert - here the comma groups thousands, and a request that names nothing means this
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

        //assert - naming nothing and naming the invariant culture are the same request
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
        //arrange - an unquoted comma belongs to no value, in any culture: it separates the arguments of a
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
        //arrange - there the fraction starts where the comma is, so `2.5` is no number: reading it as 25
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
        //arrange - and where the fraction starts at the dot, the same text is the number it says
        var hLinqQuery = Parse("""culture=en-GB&where[x.Decimal>2.5]""");

        //act
        var actual = NewApplier().Apply(entities.AsQueryable(), hLinqQuery);

        //assert
        actual.Should().BeEquivalentTo(entities.Where(x => x.Decimal > 2.5m));
    }

    private static HLinqQueryApplier NewApplier() =>
        new(ServicesCollection.Provider.GetRequiredService<IElementApplierFactory>(),
            ServicesCollection.Provider.GetRequiredService<IMethodsCache>(),
            ServicesCollection.Provider.GetRequiredService<IExpressionBuilder>());

    private static HLinqQuery<DummyEntity> Parse(string request) =>
        HLinqQuery<DummyEntity>.Parse(ServicesCollection.BinderDependenciesBag, request);
}
