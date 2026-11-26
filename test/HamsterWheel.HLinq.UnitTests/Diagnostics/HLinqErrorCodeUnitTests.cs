using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.UnitTests.Diagnostics;

/// <summary>
/// The code names a caller that is not C# compares are the contract.
/// </summary>
public class HLinqErrorCodeUnitTests
{
    [Theory]
    [InlineData(HLinqErrorCode.UnknownMember, "unknown-member")]
    [InlineData(HLinqErrorCode.ImpossibleConversion, "impossible-conversion")]
    [InlineData(HLinqErrorCode.InvalidComparison, "invalid-comparison")]
    [InlineData(HLinqErrorCode.ConstantOnlyComparison, "constant-only-comparison")]
    [InlineData(HLinqErrorCode.NotACondition, "not-a-condition")]
    [InlineData(HLinqErrorCode.InvalidMethodParameters, "invalid-method-parameters")]
    [InlineData(HLinqErrorCode.NotAnAggregate, "not-an-aggregate")]
    [InlineData(HLinqErrorCode.AggregateNeedsGroup, "aggregate-needs-group")]
    [InlineData(HLinqErrorCode.InvalidPagingValue, "invalid-paging-value")]
    [InlineData(HLinqErrorCode.DuplicateMemberName, "duplicate-member-name")]
    [InlineData(HLinqErrorCode.RootNotAllowed, "root-not-allowed")]
    [InlineData(HLinqErrorCode.InvalidQuery, "invalid-query")]
    [InlineData(HLinqErrorCode.QueryTooLong, "query-too-long")]
    [InlineData(HLinqErrorCode.TooManyProperties, "too-many-properties")]
    [InlineData(HLinqErrorCode.UnknownCulture, "unknown-culture")]
    [InlineData(HLinqErrorCode.Refused, "refused")]
    public void ToErrorString_WhenAskedForAnyCode_ThenItIsTheCodeOfIt(HLinqErrorCode code, string name)
    {
        //act + assert
        code.ToErrorString().Should().Be(name);
    }

    [Fact]
    public void ErrorCode_WhenAnErrorIsAnswered_ThenItIsTheCodeOfIt()
    {
        //arrange
        var error = HLinqError.Create(HLinqErrorCode.UnknownMember, "where", "Nope", "no", []);

        //act + assert
        error.ErrorCode.Should().Be("unknown-member");
    }
}
