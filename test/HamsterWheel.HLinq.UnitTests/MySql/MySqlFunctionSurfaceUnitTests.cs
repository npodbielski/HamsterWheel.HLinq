using FluentAssertions;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.MySql;

/// <summary>
/// The surface of the MySQL pack: every declared name of the three classes, in the set its own
/// signature puts it in. The record is a literal - when the pack widens, its record changes in the
/// same commit or this test says so.
/// </summary>
public class MySqlFunctionSurfaceUnitTests
{
    [Fact]
    public void FunctionSurface_WhenMeasured_ThenMatchesTheRecord()
    {
        //arrange
        var probe = new FunctionSurfaceProbe(new MySqlEntityFrameworkStaticMethodProvider());

        //act
        var actual = probe.Measure();

        //assert
        actual.SourceName.Should().Be("MySqlEntityFrameworkStaticMethodProvider");
        actual.ClassCounts.Should().BeEquivalentTo(
            [
                "MySqlEntityFrameworkStaticMethodProvider/DbFunctionsExtensions=2/1",
                "MySqlEntityFrameworkStaticMethodProvider/MySqlDbFunctionsExtensions=20/2",
                "MySqlEntityFrameworkStaticMethodProvider/MySqlJsonDbFunctionsExtensions=11/6"
            ],
            options => options.WithStrictOrdering());
        actual.FilterNames.Should().BeEquivalentTo(
            ["IsMatch", "JsonContains", "JsonContainsPath", "JsonOverlaps", "JsonSearchAny", "Like"],
            options => options.WithStrictOrdering());
        actual.ValueNames.Should().BeEquivalentTo(
            [
                "ConvertTimeZone",
                "DateDiffDay",
                "DateDiffHour",
                "DateDiffMicrosecond",
                "DateDiffMillisecond",
                "DateDiffMinute",
                "DateDiffMonth",
                "DateDiffNanosecond",
                "DateDiffQuarter",
                "DateDiffSecond",
                "DateDiffTick",
                "DateDiffWeek",
                "DateDiffYear",
                "Degrees",
                "JsonQuote",
                "JsonType",
                "JsonUnquote",
                "Match",
                "Radians",
                "Unhex"
            ],
            options => options.WithStrictOrdering());
        actual.OutOfReachNames.Should().BeEquivalentTo(
            [
                "AsJson: returns MySqlJsonString, which nothing of a query compares with",
                "Hex: is generic in the type of its argument, which no query names",
                "JsonContainsPathAll: parameter String[] paths binds no column and no written value",
                "JsonContainsPathAny: parameter String[] paths binds no column and no written value",
                "JsonExtract: is generic in the type of its argument, which no query names",
                "Random: no argument between the parentheses"
            ],
            options => options.WithStrictOrdering());
    }
}
