using FluentAssertions;
using HamsterWheel.HLinq.SqlServer;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.SqlServer;

/// <summary>
/// The surface of the SQL Server pack: every declared name of the provider's class, in the set its
/// own signature puts it in. The record is a literal - when the pack widens, its record changes in
/// the same commit or this test says so.
/// </summary>
public class SqlServerFunctionSurfaceUnitTests
{
    [Fact]
    public void FunctionSurface_WhenMeasured_ThenMatchesTheRecord()
    {
        //arrange
        var probe = new FunctionSurfaceProbe(new SqlServerEntityFrameworkStaticMethodProvider());

        //act
        var actual = probe.Measure();

        //assert
        actual.SourceName.Should().Be("SqlServerEntityFrameworkStaticMethodProvider");
        actual.ClassCounts.Should().BeEquivalentTo(
            ["SqlServerEntityFrameworkStaticMethodProvider/SqlServerDbFunctionsExtensions=27/4"],
            options => options.WithStrictOrdering());
        actual.FilterNames.Should().BeEquivalentTo(
            ["Contains", "FreeText", "IsDate", "IsNumeric"],
            options => options.WithStrictOrdering());
        actual.ValueNames.Should().BeEquivalentTo(
            [
                "AtTimeZone",
                "DataLength",
                "DateDiffDay",
                "DateDiffHour",
                "DateDiffMicrosecond",
                "DateDiffMillisecond",
                "DateDiffMinute",
                "DateDiffMonth",
                "DateDiffNanosecond",
                "DateDiffSecond",
                "DateDiffWeek",
                "DateDiffYear",
                "DateFromParts",
                "DateTime2FromParts",
                "DateTimeFromParts",
                "DateTimeOffsetFromParts",
                "PatIndex",
                "SmallDateTimeFromParts",
                "TimeFromParts"
            ],
            options => options.WithStrictOrdering());
        actual.OutOfReachNames.Should().BeEquivalentTo(
            [
                "StandardDeviationPopulation: parameter IEnumerable`1 values binds no column and no written value",
                "StandardDeviationSample: parameter IEnumerable`1 values binds no column and no written value",
                "VariancePopulation: parameter IEnumerable`1 values binds no column and no written value",
                "VarianceSample: parameter IEnumerable`1 values binds no column and no written value"
            ],
            options => options.WithStrictOrdering());
    }
}
