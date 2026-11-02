using FluentAssertions;
using HamsterWheel.HLinq.Sqlite;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Sqlite;

/// <summary>
/// The surface of the SQLite pack: every declared name of the generic EF class, in the set its own
/// signature puts it in. The record is a literal - when the pack widens, its record changes in the
/// same commit or this test says so.
/// </summary>
public class SqliteFunctionSurfaceUnitTests
{
    [Fact]
    public void FunctionSurface_WhenMeasured_ThenMatchesTheRecord()
    {
        //arrange
        var probe = new FunctionSurfaceProbe(new SqliteEntityFrameworkStaticMethodProvider());

        //act
        var actual = probe.Measure();

        //assert
        actual.SourceName.Should().Be("SqliteEntityFrameworkStaticMethodProvider");
        actual.ClassCounts.Should().BeEquivalentTo(
            ["SqliteEntityFrameworkStaticMethodProvider/DbFunctionsExtensions=2/1"],
            options => options.WithStrictOrdering());
        actual.FilterNames.Should().BeEquivalentTo(["Like"], options => options.WithStrictOrdering());
        actual.ValueNames.Should().BeEmpty();
        actual.OutOfReachNames.Should().BeEquivalentTo(
            ["Random: no argument between the parentheses"],
            options => options.WithStrictOrdering());
    }
}
