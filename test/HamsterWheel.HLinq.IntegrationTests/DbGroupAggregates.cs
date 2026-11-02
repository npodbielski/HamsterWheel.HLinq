using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// Aggregates over group rows against the real Postgres: the answer is what the same Linq written
/// by hand answers, and the statement the server was asked says so - <c>GROUP BY</c> with
/// <c>HAVING</c> or <c>ORDER BY agg</c> inside the grouped subquery. An aggregate evaluated
/// client-side would answer the right rows from a statement without them, so the SQL is asserted
/// beside the result.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class DbGroupAggregates(DemoFixture fixture) : IAsyncLifetime
{
    private DemoContext Context { get; set; } = null!;

    private IQueryable<Person> Persons => Context.Persons;

    public async ValueTask InitializeAsync()
    {
        Context = new DemoContext(
            new DbContextOptionsBuilder<DemoContext>().UseNpgsql(fixture.ConnectionString).Options);
        await Context.EnsureDbAndData();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    private static IHLinqQueryApplier Applier(DemoFixture fixture) =>
        fixture.DemoTestHost.Services.GetRequiredService<IHLinqQueryApplier>();

    //a last name shared by the seeded people, so a group has rows to count - picked from the
    //names read into memory, EF translates no ordering by a group's own count
    private string SharedLastName =>
        Persons.Select(p => p.LastName).ToList()
            .GroupBy(l => l)
            .OrderByDescending(g => g.Count())
            .First().Key;

    [Fact]
    public async Task WhenGroupByThenWhereCount_ThenEndpointKeepsOnlyTheWiderGroups()
    {
        //act - the classic HAVING, through the endpoint
        var response = await fixture.Client.GetAsync(
            $"/demo/db?where[x.LastName==\"{SharedLastName}\"].groupBy[x.FirstName].where[x.Count()>1]");
        response.EnsureSuccessStatusCode();

        //assert - only the groups with more than one row survive
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entries = answer.RootElement.EnumerateArray().ToArray();
        entries.Should().NotBeEmpty();
        entries.Should().OnlyContain(e => e.GetProperty("rows").GetArrayLength() > 1);
    }

    [Fact]
    public void WhenGroupByThenWhereCountRuns_ThenServerIsAskedHavingInOneStatement()
    {
        //act - the whole query through the same applier an endpoint runs
        var (sql, results) = RunAndCapture(
            "where[x.LastName==\"" + SharedLastName + "\"].groupBy[x.FirstName].where[x.Count()>1]");

        //assert - the count filtered by the server, not the rows filtered after they arrived
        sql.Should().Contain("GROUP BY");
        sql.Should().Contain("HAVING");
        sql.Should().Contain("count(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenGroupByThenSelectAggregates_ThenServerComputesThemInGroupBy()
    {
        //act
        var (sql, results) = RunAndCapture(
            "where[x.LastName==\"" + SharedLastName + "\"]" +
            ".groupBy[x.FirstName].select[C=x.Count(),S=x.Sum(x.Id),M=x.Max(x.Id),Mi=x.Min(x.Id),A=x.Average(x.Id)]");

        //assert - every aggregate is the server's work in the grouped subquery
        sql.Should().Contain("GROUP BY");
        sql.ToLowerInvariant().Should().Contain("count(");
        sql.ToLowerInvariant().Should().Contain("sum(");
        sql.ToLowerInvariant().Should().Contain("max(");
        sql.ToLowerInvariant().Should().Contain("min(");
        sql.ToLowerInvariant().Should().Contain("avg(");
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenGroupByThenOrderByAggregate_ThenServerOrdersByTheAggregate()
    {
        //act
        var (sql, results) = RunAndCapture(
            "where[x.LastName==\"" + SharedLastName + "\"]" +
            ".groupBy[x.FirstName].orderByDescending[x.Count()].take[2]");

        //assert - ORDER BY agg and LIMIT inside the grouped subquery, exactly the measured shape
        sql.Should().Contain("GROUP BY");
        sql.Should().Contain("ORDER BY").And.Contain("LIMIT");
        sql.Should().MatchRegex(@"ORDER BY count\(\*\)::int DESC(?s).*LIMIT");
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task WhenGroupByThenSelectCount_ThenAnswersCountBesideKeyAndRows()
    {
        //act
        var response = await fixture.Client.GetAsync(
            $"/demo/db?where[x.LastName==\"{SharedLastName}\"].groupBy[x.FirstName].select[C=x.Count()]");
        response.EnsureSuccessStatusCode();

        //assert - the answer holds the count beside the key and the rows of each group
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var expected = Persons
            .Where(p => p.LastName == SharedLastName)
            .GroupBy(p => p.FirstName)
            .ToDictionary(g => g.Key, g => g.Count());
        var entries = answer.RootElement.EnumerateArray()
            .ToDictionary(e => e.GetProperty("key").GetString()!, e => e.GetProperty("c").GetInt32());
        entries.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenNavigationCountInSelect_ThenServerCountsRelatedRows()
    {
        //act - the roadmap's nested count, ungrouped
        var response = await fixture.Client.GetAsync("/demo/db?where[x.Id==1].select[N=x.Addresses.Count()]");
        response.EnsureSuccessStatusCode();

        //assert
        var expected = Persons.Where(p => p.Id == 1)
            .Select(p => p.Addresses.Select(a => a.Id).Count())
            .AsEnumerable().Single();
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        answer.RootElement.EnumerateArray().Single().GetProperty("n").GetInt32().Should().Be(expected);
    }

    [Fact]
    public void WhenNavigationCountRuns_ThenServerIsAskedOneStatement()
    {
        //act
        var (sql, results) = RunAndCapture("where[x.Id==1].select[N=x.Addresses.Count()]");

        //assert - the count of the related table travels in the same statement
        sql.ToLowerInvariant().Should().Contain("count(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        results.Should().HaveCount(1);
    }

    /// <summary>
    /// The whole query through the applier an endpoint runs, over a context that records the text
    /// of every statement it executes: what came back, and what the server was actually asked.
    /// </summary>
    private (string Sql, object[] Results) RunAndCapture(string query)
    {
        var recorder = new SqlRecorder();
        using var recorded = new DemoContext(
            new DbContextOptionsBuilder<DemoContext>()
                .UseNpgsql(fixture.ConnectionString)
                .UseLoggerFactory(LoggerFactory.Create(b => b.AddProvider(recorder)))
                .Options);

        //the applier takes a query that has already been parsed; parse it the way the endpoint does
        var services = fixture.DemoTestHost.Services;
        var parsed = new HLinqQuery<Person>
        {
            SourceQueryString = query
        };
        services.GetRequiredService<IHLinqParser>().Parse(parsed,
            services.GetRequiredService<IHLinqTokenizer>().Tokenize(query));

        var results = (object[])Applier(fixture).Apply(recorded.Persons, typeof(Person), parsed);

        return (string.Join("\n", recorder.Statements), results);
    }

    /// <summary>
    /// The statements the server was actually asked, in the order it was asked them.
    /// </summary>
    private sealed class SqlRecorder : ILoggerProvider
    {
        public List<string> Statements { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Recorder(this);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private sealed class Recorder(SqlRecorder owner) : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (eventId.Id == RelationalEventId.CommandExecuted.Id)
                {
                    owner.Statements.Add(formatter(state, exception));
                }
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();

            private sealed class Scope : IDisposable
            {
                public void Dispose()
                {
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}
