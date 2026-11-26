using System.Reflection;
using DynamicAnonymousType;
using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// Two members of one name are refused while the query is parsed.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    [Theory]
    [InlineData("select[x.Text,x.Text]")]
    [InlineData("select[Z=x.Text,Z=x.Int]")]
    [InlineData("select[S=x.Children.select(Z=a.Name,Z=a.Id)]")]
    [InlineData("groupBy[x.Name].select[Rows=x.Rows.select(Z=a.Text,Z=a.Int)]")]
    public void Apply_WhenTwoMembersAnswerUnderOneName_ThenRefusedWithTheReason(string queryString)
    {
        //act
        var act = () => ApplyGrouped(queryString);

        //assert
        act.Should().Throw<MemberNameCollisionException>();
    }

    [Fact]
    public void Apply_WhenTwoValuesOfOneNameDifferInType_ThenRefusedWithAnHlinqReason()
    {
        //act
        //`Argument types do not match`, a reason that said nothing about the query
        var act = () => ApplyGrouped("select[Z=x.Text,Z=x.Int]");

        //assert
        act.Should().Throw<MemberNameCollisionException>()
            .WithMessage("'Z' answers two values - 'x.Text' and 'x.Int';*");
    }

    [Fact]
    public void WhenAShapeHoldsTwoMembersOfOneName_ThenThatNameAnswersNothing()
    {
        //arrange
        var shape = DynamicFactory.CreateType([("Z", typeof(string)), ("Z", typeof(int))]);

        //assert
        //one value, it answers none that the consumer of the answer can reach
        shape.GetProperties().Should().HaveCount(2);
        ((Func<PropertyInfo?>)(() => shape.GetProperty("Z")))
            .Should().Throw<AmbiguousMatchException>();
    }
}
