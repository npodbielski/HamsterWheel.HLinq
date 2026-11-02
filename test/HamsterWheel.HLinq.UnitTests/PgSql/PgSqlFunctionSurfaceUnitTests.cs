using FluentAssertions;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.PgSql;

/// <summary>
/// The surface of the Postgres pack: every declared name of the two Npgsql classes, in the set its
/// own signature puts it in. The record is a literal - when the pack widens, its record changes in
/// the same commit or this test says so.
/// </summary>
public class PgSqlFunctionSurfaceUnitTests
{
    [Fact]
    public void FunctionSurface_WhenMeasured_ThenMatchesTheRecord()
    {
        //arrange
        var probe = new FunctionSurfaceProbe(new PgSqlEntityFrameworkStaticMethodProvider());

        //act
        var actual = probe.Measure();

        //assert
        actual.SourceName.Should().Be("PgSqlEntityFrameworkStaticMethodProvider");
        actual.ClassCounts.Should().BeEquivalentTo(
            [
                "PgSqlEntityFrameworkStaticMethodProvider/NpgsqlDbFunctionsExtensions=10/5",
                "PgSqlEntityFrameworkStaticMethodProvider/NpgsqlTrigramsDbFunctionsExtensions=14/5"
            ],
            options => options.WithStrictOrdering());
        actual.FilterNames.Should().BeEquivalentTo(
            [
                "ILike",
                "TrigramsAreNotStrictWordSimilar",
                "TrigramsAreNotWordSimilar",
                "TrigramsAreSimilar",
                "TrigramsAreStrictWordSimilar",
                "TrigramsAreWordSimilar"
            ],
            options => options.WithStrictOrdering());
        actual.ValueNames.Should().BeEquivalentTo(
            [
                "Distance",
                "Reverse",
                "ToDate",
                "ToTimestamp",
                "TrigramsSimilarity",
                "TrigramsSimilarityDistance",
                "TrigramsStrictWordSimilarity",
                "TrigramsStrictWordSimilarityDistance",
                "TrigramsStrictWordSimilarityDistanceInverted",
                "TrigramsWordSimilarity",
                "TrigramsWordSimilarityDistance",
                "TrigramsWordSimilarityDistanceInverted"
            ],
            options => options.WithStrictOrdering());
        actual.OutOfReachNames.Should().BeEquivalentTo(
            [
                "GreaterThan: parameter ITuple a binds no column and no written value",
                "GreaterThanOrEqual: parameter ITuple a binds no column and no written value",
                "LessThan: parameter ITuple a binds no column and no written value",
                "LessThanOrEqual: parameter ITuple a binds no column and no written value",
                "StringToArray: returns String[], which nothing of a query compares with",
                "TrigramsShow: returns String[], which nothing of a query compares with"
            ],
            options => options.WithStrictOrdering());
    }
}
