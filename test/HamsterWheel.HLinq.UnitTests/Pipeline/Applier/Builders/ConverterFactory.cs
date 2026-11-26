using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier.Builders;

/// <summary>
/// An element with no registered converter is refused naming the element.
/// </summary>
public class ConverterFactoryUnitTests
{
    [Fact]
    public void GetToExpressionConverter_WhenNothingIsRegisteredForTheElement_ThenRefusedByItsName()
    {
        //arrange
        var factory = new TestServicesCollection().ConverterFactory;

        //act
        var act = () => factory.GetToExpressionConverterFor<ComparisonOperation>();

        //assert
        //is HLinq's, not the dictionary's own complaint about a missing key
        act.Should().Throw<MissingConverterException>();
    }
}
