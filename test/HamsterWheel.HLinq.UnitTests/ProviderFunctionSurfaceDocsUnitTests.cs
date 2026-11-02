using System.Text.RegularExpressions;
using FluentAssertions;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Sqlite;
using HamsterWheel.HLinq.SqlServer;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests;

/// <summary>
/// The two tables of a docs page are the prose of the two recorded sets, and its "Out of reach"
/// section the prose of the third, so a page and the record of its engine are checked against each
/// other: a page that names a call the record does not hold, or leaves a recorded name unnamed,
/// fails here instead of drifting quietly.
/// </summary>
public partial class ProviderFunctionSurfaceDocsUnitTests
{
    private static readonly (string Page, IStaticMethodSource Source, string[] ProseWords)[] Engines =
    [
        //words the page spells in backticks that name no function of the pack: the classes a pack
        //hands over and those it does not, types, and enum members
        ("postgres.md", new PgSqlEntityFrameworkStaticMethodProvider(),
            ["LIKE", "DateOnly", "DateTime", "TimeSpan", "NpgsqlInet", "IPAddress", "NpgsqlRange",
                "PhysicalAddress", "NpgsqlDbFunctionsExtensions", "NpgsqlTrigramsDbFunctionsExtensions"]),
        ("sql-server.md", new SqlServerEntityFrameworkStaticMethodProvider(),
            ["DateTime", "DateTimeOffset", "TimeOnly", "DateOnly", "TimeSpan", "byte", "Guid",
                "SqlServerDbFunctionsExtensions", "DateDiff"]),
        ("mysql.md", new MySqlEntityFrameworkStaticMethodProvider(),
            ["NaturalLanguage", "NaturalLanguageWithQueryExpansion", "Boolean", "DbFunctionsExtensions",
                "MySqlDbFunctionsExtensions", "MySqlJsonDbFunctionsExtensions"]),
        ("sqlite.md", new SqliteEntityFrameworkStaticMethodProvider(),
            ["Glob", "Hex", "Unhex", "Substr", "DbFunctionsExtensions", "SqliteDbFunctionsExtensions"])
    ];

    [Theory]
    [MemberData(nameof(EnginePages))]
    public void GivenEnginePage_WhenReadAgainstTheRecord_ThenNeitherNamesWhatTheOtherDoesNot(
        string page, FunctionSurface surface, string[] proseWords)
    {
        //act
        var text = ReadPage(page);
        var called = CalledNames(text);
        var backticked = BacktickedNames(text);

        //assert
        var named = backticked.Concat(called).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var recorded in surface.FilterNames.Concat(surface.ValueNames)
                     .Concat(surface.OutOfReachNames.Select(RecordedName)))
        {
            named.Should().Contain(recorded, $"the page names the recorded call {recorded}");
        }

        foreach (var call in called)
        {
            surface.FilterNames.Concat(surface.ValueNames)
                .Should().Contain(call, $"the page writes a reachable call {call} the record holds");
        }

        var unnamed = backticked
            .Where(name => !surface.FilterNames.Contains(name, StringComparer.OrdinalIgnoreCase)
                           && !surface.ValueNames.Contains(name, StringComparer.OrdinalIgnoreCase)
                           && !surface.OutOfReachNames.Select(RecordedName)
                               .Contains(name, StringComparer.OrdinalIgnoreCase)
                           && !proseWords.Contains(name, StringComparer.OrdinalIgnoreCase));
        unnamed.Should().BeEmpty("every name the page spells in backticks is a recorded name or a listed prose word");
    }

    public static TheoryData<string, FunctionSurface, string[]> EnginePages()
    {
        var data = new TheoryData<string, FunctionSurface, string[]>();
        foreach (var (page, source, proseWords) in Engines)
        {
            data.Add(page, new FunctionSurfaceProbe(source).Measure(), proseWords);
        }

        return data;
    }

    private static string RecordedName(string outOfReach) => outOfReach.Split(':', 2)[0].Trim();

    private static SortedSet<string> CalledNames(string text) =>
        [.. Called().Matches(text).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];

    private static SortedSet<string> BacktickedNames(string text) =>
        [.. Backtick().Matches(text).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];

    private static string ReadPage(string page)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HLinq.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the test runs from a checkout of this repository");
        var path = Path.Combine(directory.FullName, "docs", "db-functions", page);
        File.Exists(path).Should().BeTrue($"the page of the engine is {path}");

        return File.ReadAllText(path);
    }

    [GeneratedRegex(@"where\[(\w+)\(")]
    private static partial Regex Called();

    [GeneratedRegex(@"`(\w+)`")]
    private static partial Regex Backtick();
}
